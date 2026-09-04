using FluentAssertions;
using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Services.Storage;
using Microsoft.Extensions.Options;

namespace MerchForge.UnitTests.Services.Storage;

/// <summary>
/// The key format and the key/URL boundary. These are the rules that keep one
/// business out of another business's images, so the cross-tenant cases matter as
/// much as the happy path.
/// </summary>
public class ProductImageUrlResolverTests
{
    private const string PublicBaseUrl = "https://pub-test.r2.dev";

    private static readonly Guid BusinessId = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid OtherBusinessId = Guid.Parse("22222222-2222-4222-8222-222222222222");
    private static readonly Guid ProductId = Guid.Parse("33333333-3333-4333-8333-333333333333");

    private readonly ProductImageUrlResolver _resolver = new(
        Options.Create(new R2Options
        {
            AccountId = "account",
            AccessKeyId = "key-id",
            SecretAccessKey = "secret",
            BucketName = "bucket",
            Endpoint = "https://account.r2.cloudflarestorage.com",

            // Trailing slash on purpose: the resolver has to cope with it rather than
            // emitting a doubled slash into every image URL.
            PublicBaseUrl = PublicBaseUrl + "/",
        }),
        Options.Create(new ProductImageOptions()));

    // ---- BuildKey ----

    [Fact]
    public void BuildKey_nests_images_under_their_business_and_product()
    {
        var key = _resolver.BuildKey(BusinessId, ProductId, ".jpg");

        key.Should().StartWith($"businesses/{BusinessId}/products/{ProductId}/images/");
        key.Should().EndWith(".jpg");
    }

    /// <summary>
    /// The image id must come from the server. A key derived from anything the client
    /// controls is how an upload overwrites somebody else's object.
    /// </summary>
    [Fact]
    public void BuildKey_generates_a_fresh_image_id_every_time()
    {
        var first = _resolver.BuildKey(BusinessId, ProductId, ".png");
        var second = _resolver.BuildKey(BusinessId, ProductId, ".png");

        first.Should().NotBe(second);
    }

    // ---- ToPublicUrl ----

    [Fact]
    public void ToPublicUrl_prefixes_a_key_with_the_configured_public_origin()
    {
        var key = $"businesses/{BusinessId}/products/{ProductId}/images/{Guid.NewGuid()}.jpg";

        _resolver.ToPublicUrl(key).Should().Be($"{PublicBaseUrl}/{key}");
    }

    [Fact]
    public void ToPublicUrl_leaves_a_pre_migration_local_path_alone()
    {
        const string legacy = "/uploads/products/abc/def.jpg";

        _resolver.ToPublicUrl(legacy).Should().Be(legacy);
    }

    /// <summary>
    /// Resolution is applied at a dozen projection sites; being idempotent means a
    /// site that resolves twice produces the right URL rather than a doubled origin.
    /// </summary>
    [Fact]
    public void ToPublicUrl_is_idempotent()
    {
        var key = $"businesses/{BusinessId}/products/{ProductId}/images/{Guid.NewGuid()}.webp";

        var once = _resolver.ToPublicUrl(key);

        _resolver.ToPublicUrl(once).Should().Be(once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToPublicUrl_passes_through_absent_values(string? stored)
    {
        _resolver.ToPublicUrl(stored).Should().Be(stored);
    }

    // ---- ToStorageKey ----

    [Fact]
    public void ToStorageKey_recovers_the_key_from_a_public_url()
    {
        var key = $"businesses/{BusinessId}/products/{ProductId}/images/{Guid.NewGuid()}.jpg";

        _resolver.ToStorageKey($"{PublicBaseUrl}/{key}", BusinessId).Should().Be(key);
    }

    [Fact]
    public void ToStorageKey_accepts_a_bare_key()
    {
        var key = $"businesses/{BusinessId}/products/{ProductId}/images/{Guid.NewGuid()}.png";

        _resolver.ToStorageKey(key, BusinessId).Should().Be(key);
    }

    /// <summary>
    /// The whole point of parsing the path shape instead of the configured origin: a
    /// URL issued before a move to a custom image domain still resolves afterwards.
    /// </summary>
    [Fact]
    public void ToStorageKey_accepts_a_url_from_a_different_origin_than_the_configured_one()
    {
        var key = $"businesses/{BusinessId}/products/{ProductId}/images/{Guid.NewGuid()}.jpg";

        _resolver.ToStorageKey($"https://cdn.merchforge.example/{key}", BusinessId).Should().Be(key);
    }

    [Fact]
    public void ToStorageKey_round_trips_a_built_key_through_its_public_url()
    {
        var key = _resolver.BuildKey(BusinessId, ProductId, ".webp");

        _resolver.ToStorageKey(_resolver.ToPublicUrl(key), BusinessId).Should().Be(key);
    }

    [Fact]
    public void ToStorageKey_keeps_a_pre_migration_local_path_for_this_business()
    {
        var legacy = $"/uploads/products/{BusinessId}/abc123.jpg";

        _resolver.ToStorageKey(legacy, BusinessId).Should().Be(legacy);
    }

    /// <summary>
    /// Images carried over from local disk that no product ever claimed - referenced
    /// only by an edit job - land under legacy-images, because inventing a product id
    /// would put a lie in the part of the key that is meant to be trustworthy. They
    /// still have to be editable afterwards.
    /// </summary>
    [Fact]
    public void ToStorageKey_accepts_a_carried_over_image_with_no_product()
    {
        var key = $"businesses/{BusinessId}/legacy-images/{Guid.NewGuid()}.jpg";

        _resolver.ToStorageKey(key, BusinessId).Should().Be(key);
    }

    [Fact]
    public void ToStorageKey_rejects_a_carried_over_image_from_another_business()
    {
        var key = $"businesses/{OtherBusinessId}/legacy-images/{Guid.NewGuid()}.jpg";

        var act = () => _resolver.ToStorageKey(key, BusinessId);

        act.Should().Throw<InvalidProductImageException>();
    }

    // ---- ToStorageKey: rejections ----

    [Fact]
    public void ToStorageKey_rejects_a_key_belonging_to_another_business()
    {
        var key = $"businesses/{OtherBusinessId}/products/{ProductId}/images/{Guid.NewGuid()}.jpg";

        var act = () => _resolver.ToStorageKey(key, BusinessId);

        act.Should().Throw<InvalidProductImageException>()
            .WithMessage("That image does not belong to this business.");
    }

    [Fact]
    public void ToStorageKey_rejects_a_pre_migration_path_belonging_to_another_business()
    {
        var legacy = $"/uploads/products/{OtherBusinessId}/abc123.jpg";

        var act = () => _resolver.ToStorageKey(legacy, BusinessId);

        act.Should().Throw<InvalidProductImageException>();
    }

    [Theory]
    // Traversal, in both the legacy and the key shape.
    [InlineData("/uploads/products/../../appsettings.json")]
    [InlineData("businesses/../../secrets")]
    // Right prefix, wrong depth.
    [InlineData("businesses/11111111-1111-4111-8111-111111111111/images/x.jpg")]
    // Business segment is not a guid.
    [InlineData("businesses/not-a-guid/products/p/images/i.jpg")]
    // Not an image extension.
    [InlineData("businesses/11111111-1111-4111-8111-111111111111/products/33333333-3333-4333-8333-333333333333/images/44444444-4444-4444-8444-444444444444.svg")]
    // Image id is not a guid, so not something this app ever wrote.
    [InlineData("businesses/11111111-1111-4111-8111-111111111111/products/33333333-3333-4333-8333-333333333333/images/payload.jpg")]
    // Someone else entirely.
    [InlineData("https://evil.example/businesses/x/products/y/images/z.jpg")]
    [InlineData("../../etc/passwd")]
    [InlineData("   ")]
    public void ToStorageKey_rejects_anything_it_does_not_recognise(string incoming)
    {
        var act = () => _resolver.ToStorageKey(incoming, BusinessId);

        act.Should().Throw<InvalidProductImageException>();
    }

    /// <summary>
    /// A non-http scheme must never reach the storage layer, however well-formed the
    /// rest of the value looks.
    /// </summary>
    [Fact]
    public void ToStorageKey_rejects_a_non_http_scheme()
    {
        var key = $"businesses/{BusinessId}/products/{ProductId}/images/{Guid.NewGuid()}.jpg";

        var act = () => _resolver.ToStorageKey($"file:///{key}", BusinessId);

        act.Should().Throw<InvalidProductImageException>();
    }

    // ---- IsLegacyLocalPath ----

    [Theory]
    [InlineData("/uploads/products/abc/def.jpg", true)]
    [InlineData("businesses/a/products/b/images/c.jpg", false)]
    [InlineData("https://pub-test.r2.dev/businesses/a/products/b/images/c.jpg", false)]
    [InlineData(null, false)]
    public void IsLegacyLocalPath_distinguishes_disk_from_bucket(string? stored, bool expected)
    {
        _resolver.IsLegacyLocalPath(stored).Should().Be(expected);
    }
}
