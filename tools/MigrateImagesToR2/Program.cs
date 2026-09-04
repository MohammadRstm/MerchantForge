using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using MerchForge.api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

// Moves images that predate the R2 migration off local disk and into the bucket,
// rewrites every database reference to them, and only then removes the local file.
//
// Ordering is the whole point. An object is uploaded and read back before any row is
// rewritten, and a file is deleted only once its row no longer points at it. A run
// that dies halfway leaves objects in the bucket that nothing references yet, which
// costs a little storage; it never leaves a row pointing at something that is gone.
//
// Safe to run more than once: anything already migrated is skipped.
//
//   dotnet run --project tools/MigrateImagesToR2                    report only
//   dotnet run --project tools/MigrateImagesToR2 -- --apply         upload + rewrite rows
//   dotnet run --project tools/MigrateImagesToR2 -- --apply --delete-local   also remove files

var apply = args.Contains("--apply");
var deleteLocal = args.Contains("--delete-local");

if (deleteLocal && !apply)
{
    Console.WriteLine("--delete-local requires --apply: files are only removed once their rows have moved.");
    return 1;
}

var config = new ConfigurationBuilder()
    .SetBasePath(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../MerchForge.api")))
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    // GetExecutingAssembly rather than typeof(Program): MerchForge.api has a Program
    // of its own, so that reference is ambiguous and could read the wrong assembly's
    // UserSecretsId attribute.
    .AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly(), optional: true)
    .AddEnvironmentVariables()
    .Build();

string Require(string key)
{
    var value = config[key];

    if (string.IsNullOrWhiteSpace(value))
    {
        throw new InvalidOperationException($"{key} is not configured.");
    }

    return value;
}

var connectionString = Require("ConnectionStrings:DefaultConnection");
var bucket = Require("R2:BucketName");

var s3 = new AmazonS3Client(
    new BasicAWSCredentials(Require("R2:AccessKeyId"), Require("R2:SecretAccessKey")),
    new AmazonS3Config
    {
        ServiceURL = Require("R2:Endpoint"),
        AuthenticationRegion = "auto",
        ForcePathStyle = true,
        RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
        ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
    });

// The web root the API serves files from, which is where every local path resolves.
var webRoot = Path.GetFullPath(Path.Combine(
    AppContext.BaseDirectory, "../../../../../MerchForge.api/wwwroot"));

var options = new DbContextOptionsBuilder<MerchForgeDbContext>()
    .UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString),
        mySql => mySql.UseMicrosoftJson())
    .Options;

await using var db = new MerchForgeDbContext(options);

Console.WriteLine($"mode      {(apply ? (deleteLocal ? "apply + delete local" : "apply") : "report only (pass --apply to write)")}");
Console.WriteLine($"bucket    {bucket}");
Console.WriteLine($"web root  {webRoot}");
Console.WriteLine();

// ---- 1. collect every local reference, and what it belongs to --------------------

// A local value is anything the API still serves itself: it starts with "/".
static bool IsLocal(string? value) => !string.IsNullOrWhiteSpace(value) && value.StartsWith('/');

// path -> the business and product it belongs to. Product is nullable: a handful of
// images are only ever referenced by an edit job, which has no product.
var owners = new Dictionary<string, (Guid BusinessId, Guid? ProductId)>(StringComparer.Ordinal);

void Note(string? path, Guid businessId, Guid? productId)
{
    if (!IsLocal(path))
    {
        return;
    }

    // First writer wins, except that a known product beats an unknown one.
    if (owners.TryGetValue(path!, out var existing) && (existing.ProductId is not null || productId is null))
    {
        return;
    }

    owners[path!] = (businessId, productId);
}

// Product galleries are the authoritative attribution: the row names its product.
foreach (var image in await db.ProductImages
    .AsNoTracking()
    .Select(i => new { i.Url, i.ProductId, i.Product.BusinessId })
    .ToListAsync())
{
    Note(image.Url, image.BusinessId, image.ProductId);
}

foreach (var product in await db.Products
    .AsNoTracking()
    .Where(p => p.ImageUrl != null)
    .Select(p => new { p.ImageUrl, p.Id, p.BusinessId })
    .ToListAsync())
{
    Note(product.ImageUrl, product.BusinessId, product.Id);
}

// Order items are historical snapshots, and their product still exists because a
// product with orders cannot be deleted.
foreach (var item in await db.OrderItems
    .AsNoTracking()
    .Where(i => i.ProductImageUrl != null)
    .Select(i => new { i.ProductImageUrl, i.ProductId, i.Order.BusinessId })
    .ToListAsync())
{
    Note(item.ProductImageUrl, item.BusinessId, item.ProductId);
}

// A draft becomes exactly one product, created with the draft's own id, so that id
// is the truthful product for its images.
foreach (var draft in await db.ProductDrafts
    .AsNoTracking()
    .Select(d => new { d.Id, d.BusinessId, d.OriginalImageUrl, d.ProcessedImageUrl })
    .ToListAsync())
{
    Note(draft.OriginalImageUrl, draft.BusinessId, draft.Id);
    Note(draft.ProcessedImageUrl, draft.BusinessId, draft.Id);
}

// Edit jobs come last, and only fill in what nothing else claimed. An output nobody
// applied belongs to no product, which is what the legacy key family is for.
var editJobs = await db.ImageEditJobs
    .AsNoTracking()
    .Select(j => new { j.Id, j.BusinessId, j.OutputImageUrl, InputJson = j.InputImageUrls })
    .ToListAsync();

foreach (var job in editJobs)
{
    Note(job.OutputImageUrl, job.BusinessId, null);

    foreach (var input in ReadUrls(job.InputJson))
    {
        Note(input, job.BusinessId, null);
    }
}

var templatePreviews = await db.WebsiteTemplates
    .AsNoTracking()
    .Select(t => new { t.Id, t.PreviewImageUrl })
    .ToListAsync();

static IEnumerable<string> ReadUrls(JsonDocument? document)
{
    if (document is null || document.RootElement.ValueKind != JsonValueKind.Array)
    {
        yield break;
    }

    foreach (var element in document.RootElement.EnumerateArray())
    {
        if (element.GetString() is { } url)
        {
            yield return url;
        }
    }
}

// ---- 2. decide each path's new key ----------------------------------------------

// Template previews are global, and the seeded /images/templates/coming-soon.jpg is a
// bundled asset rather than an upload, so it is left exactly where it is.
var templatePaths = templatePreviews
    .Where(t => IsLocal(t.PreviewImageUrl) && t.PreviewImageUrl.StartsWith("/uploads/", StringComparison.Ordinal))
    .Select(t => t.PreviewImageUrl)
    .Distinct(StringComparer.Ordinal)
    .ToList();

var plan = new Dictionary<string, string>(StringComparer.Ordinal);

foreach (var (path, owner) in owners)
{
    var extension = Path.GetExtension(path).ToLowerInvariant();

    plan[path] = owner.ProductId is { } productId
        ? $"businesses/{owner.BusinessId}/products/{productId}/images/{Guid.NewGuid()}{extension}"
        // No product to nest under, and inventing one would put a lie in the very
        // part of the key that is supposed to be trustworthy.
        : $"businesses/{owner.BusinessId}/legacy-images/{Guid.NewGuid()}{extension}";
}

foreach (var path in templatePaths)
{
    plan[path] = $"website-templates/{Guid.NewGuid()}{Path.GetExtension(path).ToLowerInvariant()}";
}

Console.WriteLine($"{plan.Count} local image reference(s) found in the database.");

// Preflight, so a permissions problem is named up front rather than inferred from
// dozens of identical failures. Read and write are checked separately: an R2 API
// token can be created read-only, and that is the difference between "wrong
// credentials" and "right credentials, wrong permission".
if (apply)
{
    var canRead = false;

    try
    {
        await s3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, MaxKeys = 1 });
        canRead = true;
        Console.WriteLine("bucket read  ok");
    }
    catch (AmazonS3Exception ex)
    {
        Console.WriteLine($"bucket read  FAILED ({ex.ErrorCode}: {ex.Message})");
    }

    var probeKey = $"diagnostics/write-check-{Guid.NewGuid()}";

    try
    {
        var probe = new PutObjectRequest
        {
            BucketName = bucket,
            Key = probeKey,
            ContentBody = "write check",
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true,
        };

        await s3.PutObjectAsync(probe);
        await s3.DeleteObjectAsync(bucket, probeKey);

        Console.WriteLine("bucket write ok");
        Console.WriteLine();
    }
    catch (AmazonS3Exception ex)
    {
        Console.WriteLine($"bucket write FAILED ({ex.ErrorCode}: {ex.Message})");
        Console.WriteLine();
        Console.WriteLine("Nothing has been uploaded or changed.");

        if (canRead)
        {
            Console.WriteLine();
            Console.WriteLine("Reads work and writes do not, which means the credentials are correct and the");
            Console.WriteLine("R2 API token is missing write permission. In the Cloudflare dashboard, under");
            Console.WriteLine("R2 > Manage R2 API Tokens, the token needs Object Read & Write rather than");
            Console.WriteLine($"Object Read only, and its bucket scope has to cover {bucket}.");
        }

        return 1;
    }
}

// ---- 3. upload, and read each one back before anything is rewritten --------------

var uploaded = new Dictionary<string, string>(StringComparer.Ordinal);
var missing = new List<string>();
var failed = new List<string>();

foreach (var (path, key) in plan.OrderBy(p => p.Key, StringComparer.Ordinal))
{
    var absolute = Path.Combine(webRoot, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

    if (!File.Exists(absolute))
    {
        // Already broken before this ran. Left alone rather than quietly repointed.
        missing.Add(path);
        continue;
    }

    if (!apply)
    {
        uploaded[path] = key;
        continue;
    }

    try
    {
        var bytes = await File.ReadAllBytesAsync(absolute);

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            InputStream = new MemoryStream(bytes),
            ContentType = ContentTypeFor(Path.GetExtension(absolute)),
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true,
        };
        request.Headers.CacheControl = "public, max-age=31536000, immutable";

        await s3.PutObjectAsync(request);

        // Read back rather than trusting the write. This is what makes deleting the
        // local copy afterwards defensible.
        using var check = await s3.GetObjectAsync(bucket, key);
        using var buffer = new MemoryStream();
        await check.ResponseStream.CopyToAsync(buffer);

        if (!buffer.ToArray().SequenceEqual(bytes))
        {
            failed.Add($"{path} (round-tripped bytes differ)");
            continue;
        }

        uploaded[path] = key;
    }
    catch (Exception ex)
    {
        failed.Add($"{path} ({ex.GetType().Name}: {ex.Message})");
    }
}

static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
{
    ".jpg" or ".jpeg" => "image/jpeg",
    ".png" => "image/png",
    ".gif" => "image/gif",
    ".webp" => "image/webp",
    ".ico" => "image/x-icon",
    _ => "application/octet-stream",
};

Console.WriteLine($"{uploaded.Count} uploaded and verified, {missing.Count} missing on disk, {failed.Count} failed.");

if (!apply)
{
    Console.WriteLine();
    Console.WriteLine("Report only. Nothing was uploaded, rewritten or deleted.");
    Report();
    return 0;
}

if (failed.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Stopping before touching the database, because some uploads failed:");
    failed.ForEach(f => Console.WriteLine($"    {f}"));
    return 1;
}

// ---- 4. rewrite every reference, in one transaction ------------------------------

string? Rewrite(string? value) =>
    value is not null && uploaded.TryGetValue(value, out var key) ? key : value;

await using (var transaction = await db.Database.BeginTransactionAsync())
{
    foreach (var image in await db.ProductImages.ToListAsync())
    {
        image.Url = Rewrite(image.Url)!;
    }

    foreach (var product in await db.Products.Where(p => p.ImageUrl != null).ToListAsync())
    {
        product.ImageUrl = Rewrite(product.ImageUrl);
    }

    foreach (var item in await db.OrderItems.Where(i => i.ProductImageUrl != null).ToListAsync())
    {
        item.ProductImageUrl = Rewrite(item.ProductImageUrl);
    }

    foreach (var draft in await db.ProductDrafts.ToListAsync())
    {
        draft.OriginalImageUrl = Rewrite(draft.OriginalImageUrl);
        draft.ProcessedImageUrl = Rewrite(draft.ProcessedImageUrl);
    }

    // The input list is a json column, so this is a rewrite of the document rather
    // than a column update.
    foreach (var job in await db.ImageEditJobs.ToListAsync())
    {
        job.OutputImageUrl = Rewrite(job.OutputImageUrl);

        var inputs = ReadUrls(job.InputImageUrls).Select(u => Rewrite(u)!).ToList();

        job.InputImageUrls = JsonSerializer.SerializeToDocument(inputs);
    }

    foreach (var template in await db.WebsiteTemplates.ToListAsync())
    {
        template.PreviewImageUrl = Rewrite(template.PreviewImageUrl)!;
    }

    await db.SaveChangesAsync();
    await transaction.CommitAsync();
}

Console.WriteLine("Database references rewritten.");

// ---- 5. only now, remove the local files -----------------------------------------

var deleted = 0;

if (deleteLocal)
{
    foreach (var path in uploaded.Keys)
    {
        var absolute = Path.Combine(webRoot, path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

        try
        {
            File.Delete(absolute);
            deleted++;
        }
        catch (Exception ex)
        {
            // The row already points at the bucket, so a file left behind is only
            // clutter. Worth reporting, not worth failing over.
            Console.WriteLine($"    could not delete {path}: {ex.Message}");
        }
    }

    Console.WriteLine($"{deleted} local file(s) deleted.");
}
else
{
    Console.WriteLine("Local files left in place. Re-run with --delete-local once you are satisfied.");
}

Report();
return 0;

void Report()
{
    if (missing.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"{missing.Count} reference(s) point at a file that is not on disk. These were already");
        Console.WriteLine("broken and have been left untouched rather than silently repointed:");
        missing.Take(20).ToList().ForEach(m => Console.WriteLine($"    {m}"));

        if (missing.Count > 20)
        {
            Console.WriteLine($"    ... and {missing.Count - 20} more");
        }
    }

    var legacy = plan.Count(p => p.Value.Contains("/legacy-images/", StringComparison.Ordinal));

    if (legacy > 0)
    {
        Console.WriteLine();
        Console.WriteLine($"{legacy} image(s) had no product to nest under - referenced only by an edit job -");
        Console.WriteLine("and went to businesses/{businessId}/legacy-images/ instead.");
    }
}
