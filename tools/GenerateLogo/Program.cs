// One-off script: generates MerchForge logo concepts with Gemini's image model
// ("Nano Banana 2") using the same API key MerchForge.api already has configured
// under GeminiFlash:apiKey. Not part of the app - lives outside MerchForge.api's
// project folder on purpose so it isn't picked up by its build, and isn't wired
// into MerchForge.slnx since it's a throwaway tool, not a project of the solution.
//
// Run from the repo root:
//   dotnet run --project tools/GenerateLogo
//
// Optional: pass a model id to override the default, e.g.
//   dotnet run --project tools/GenerateLogo -- gemini-3.1-flash-image

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddUserSecrets(typeof(Program).Assembly)
    .AddEnvironmentVariables()
    .Build();

var apiKey = config["GeminiFlash:apiKey"];
if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine(
        "No Gemini API key found. Expected GeminiFlash:apiKey in user secrets " +
        "(same store as MerchForge.api - run `dotnet user-secrets list --project MerchForge.api` " +
        "to confirm it's there) or a GeminiFlash__apiKey environment variable.");
    return 1;
}

// "Nano Banana 2 Pro" in MerchForge.api's own naming (see GeminiOptions.cs) -
// worth the higher cost here since this runs a handful of times total, not per
// user request like the in-app AI features.
var model = args.Length > 0 ? args[0] : "gemini-3-pro-image";
const string baseUrl = "https://generativelanguage.googleapis.com/v1beta/";

using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
http.DefaultRequestHeaders.TryAddWithoutValidation("x-goog-api-key", apiKey);

var brandBrief =
    "Design a logo mark for MerchForge, a SaaS platform that lets small merchants " +
    "spin up their own AI-assisted e-commerce storefront - think \"Shopify meets an " +
    "AI co-founder for product listings.\" The name fuses \"merchant\" and \"forge\": " +
    "the product idea is that raw material (a photo, a voice note, a rough idea) gets " +
    "shaped into a finished, sellable storefront. Lean into that forging/crafting " +
    "metaphor - construction, shaping, smithing - rather than generic shopping-cart " +
    "or bag iconography.\n\n" +
    "Brand colors: near-black (#14161B) as the dominant/ground color, a warm " +
    "construction-orange (#FF9B00) as a single sharp accent - used sparingly, like a " +
    "spark, ember, or forge-glow, not as a second dominant color. Background is a " +
    "warm off-white/cream (#F2ECE2), not pure white.\n\n" +
    "Typography feel: the brand's headline typeface is Fraunces, an editorial, " +
    "slightly warm serif with high-contrast strokes - if the mark includes a " +
    "wordmark or letterform, it should feel like it belongs next to that typeface " +
    "(confident, a little crafted, not geometric-sans-startup-generic).\n\n" +
    "Existing placeholder to riff on, not copy: a rounded near-black square " +
    "containing a simple white \"M\" built from two angled strokes, with a small " +
    "solid orange circle sitting at the upper-right corner like a spark or sun. The " +
    "new logo should feel like a natural evolution of that idea - same restraint, " +
    "same \"one accent, one gesture\" discipline - not a completely unrelated " +
    "direction.\n\n" +
    "Generate a single, simple, high-contrast icon/symbol (not a full illustrated " +
    "scene) that works as a square app icon down to 32px and reads clearly as a " +
    "favicon. Flat, vector-style, 2-3 colors max, no gradients, no photorealism, no " +
    "drop shadows, no text other than a possible single letterform. Square canvas, " +
    "generous padding around the mark.\n\n" +
    "Avoid: shopping carts, shopping bags, generic marketplace/storefront clipart, " +
    "rocket ships, generic \"AI sparkle\" starburst cliches, gradients, 3D bevels, " +
    "more than one accent color.";

// Four distinct concepts rather than four identical calls, so the batch actually
// gives Mohammad something to choose between instead of four near-duplicates.
var concepts = new (string Name, string Focus)[]
{
    ("anvil-spark", "Concept: an anvil (or an abstracted anvil silhouette) with a single orange spark leaping off it."),
    ("m-forge", "Concept: the existing white \"M\" mark, redrawn so one stroke also reads as a hammer or forge tool, orange spark at the point of impact."),
    ("storefront-spark", "Concept: an abstract, minimal storefront/awning silhouette catching a single orange spark or ember above it."),
    ("flame-anvil", "Concept: a small stylized flame or ember shape sitting on/rising from a simple anvil base, entirely in the two-color palette."),
};

var outputDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "output");
Directory.CreateDirectory(outputDir);
outputDir = Path.GetFullPath(outputDir);

Console.WriteLine($"Model: {model}");
Console.WriteLine($"Output: {outputDir}\n");

var succeeded = 0;

foreach (var (name, focus) in concepts)
{
    Console.Write($"Generating '{name}'... ");

    try
    {
        var bytes = await GenerateImage(http, model, $"{brandBrief}\n\n{focus}");
        var path = Path.Combine(outputDir, $"logo-{name}.png");
        await File.WriteAllBytesAsync(path, bytes);
        Console.WriteLine($"saved -> {path}");
        succeeded++;
    }
    catch (Exception ex)
    {
        // Printed and moved on rather than aborting the batch - one concept failing
        // (rate limit, transient 5xx) shouldn't cost the other three.
        Console.WriteLine($"FAILED: {ex.Message}");
    }
}

Console.WriteLine($"\n{succeeded}/{concepts.Length} generated.");
return succeeded > 0 ? 0 : 1;

static async Task<byte[]> GenerateImage(HttpClient http, string model, string prompt)
{
    var payload = new RawInteractionRequest
    {
        Model = model,
        Input = [new RawTextInputPart { Text = prompt }],
    };

    using var request = new HttpRequestMessage(HttpMethod.Post, "interactions")
    {
        Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"),
    };

    using var response = await http.SendAsync(request);
    var body = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException($"HTTP {(int)response.StatusCode}: {body}");
    }

    var parsed = JsonSerializer.Deserialize<RawInteractionResponse>(
        body,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    var part = parsed?.OutputImage;

    if (part?.Data is null && parsed?.Steps is not null)
    {
        part = parsed.Steps
            .AsEnumerable()
            .Reverse()
            .SelectMany(step => step.Content ?? [])
            .FirstOrDefault(item => item.Type == "image" && item.Data is not null);
    }

    if (part?.Data is null)
    {
        throw new InvalidOperationException($"No image in response: {body}");
    }

    return Convert.FromBase64String(part.Data);
}

// Same wire shape MerchForge.api's GeminiImageEditingClient already uses against
// this endpoint - kept identical here rather than referencing the API project, so
// this stays a standalone, disposable script.
internal sealed class RawInteractionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public List<object> Input { get; set; } = [];
}

internal sealed class RawTextInputPart
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

internal sealed class RawInteractionResponse
{
    [JsonPropertyName("output_image")]
    public RawImagePart? OutputImage { get; set; }

    [JsonPropertyName("steps")]
    public List<RawStep>? Steps { get; set; }
}

internal sealed class RawStep
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("content")]
    public List<RawImagePart>? Content { get; set; }
}

internal sealed class RawImagePart
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }

    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }
}
