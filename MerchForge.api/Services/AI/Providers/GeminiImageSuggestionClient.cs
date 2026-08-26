using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.AI;
using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.AI.Interfaces;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Services.AI.Providers;

/// <summary>
/// Google's Gemini image models via the same Interactions API
/// (POST {baseUrl}interactions) GeminiImageEditingClient uses, asked for text
/// instead of pixels: one product photo in, a best-effort filled-in draft out.
///
/// The request shape is identical to the editing client's (N image parts + one
/// text part) — only the instruction and the response parsing differ, since here
/// the answer is a JSON object in a "text" content part rather than an image.
///
/// No retries, same reasoning as the editing client: a failed or unreadable
/// answer surfaces to the owner rather than silently multiplying provider cost.
/// </summary>
public class GeminiImageSuggestionClient : IProductImageSuggestionClient
{
    private static readonly JsonSerializerOptions ResponseJson = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly GeminiOptions _options;

    public GeminiImageSuggestionClient(HttpClient http, IOptions<GeminiOptions> options)
    {
        _options = options.Value;
        _http = http;

        _http.BaseAddress ??= new Uri(_options.BaseUrl);

        _http.DefaultRequestHeaders.Remove("x-goog-api-key");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
    }

    public string ModelName => _options.ImageEditingModel;

    public async Task<ProductAiDraft> SuggestAsync(
        ImageEditInput image,
        ProductAiContext context,
        CancellationToken cancellationToken = default)
    {
        var payload = new RawInteractionRequest
        {
            Model = _options.ImageEditingModel,
            Input =
            [
                new RawImageInputPart { MimeType = image.MimeType, Data = Convert.ToBase64String(image.Bytes) },
                new RawTextInputPart { Text = BuildInstruction(context) },
            ],
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "interactions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"),
        };

        using var response = await _http.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ImageEditingException(
                $"The image analysis provider returned status {(int)response.StatusCode}.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return ParseSuggestion(body);
    }

    /// <summary>
    /// Lists this business's real categories and metadata fields so the model can
    /// only ever propose values that actually exist for it, and tells it explicitly
    /// to leave anything it can't see in the photo null rather than guess — the
    /// same "fill strictly what you can determine" instruction the voice flow's
    /// prompt carries, just aimed at a photo instead of a spoken description.
    /// </summary>
    private static string BuildInstruction(ProductAiContext context)
    {
        var categories = context.Categories.Count > 0
            ? string.Join("\n", context.Categories.Select(c => $"- {c.Id}: {c.Name}"))
            : "(none configured)";

        var fields = context.MetadataFields.Count > 0
            ? string.Join("\n", context.MetadataFields.Select(f =>
                f.AllowedValues.Count > 0
                    ? $"- key \"{f.Key}\" (\"{f.Label}\"), type {f.ValueType}, allowed values: {string.Join(", ", f.AllowedValues)}"
                    : $"- key \"{f.Key}\" (\"{f.Label}\"), type {f.ValueType}"))
            : "(none configured)";

        const string jsonShape = """
            {"title": string|null, "description": string|null, "price": number|null,
            "compareAtPrice": number|null, "categoryId": string|null, "sku": string|null,
            "stockQuantity": integer|null, "tags": string[], "saleEndsAt": string|null,
            "metadata": object|null}
            """;

        return $"""
            You are looking at a single product photo for "{context.BusinessName}". Based only on
            what is visible in the photo, fill in as many of the following product fields as you
            can genuinely determine. Leave a field null when you cannot tell from the photo alone
            — do not guess. Price, compareAtPrice, sku, stockQuantity, and saleEndsAt are almost
            never visible in a product photo; only fill one of them if it is literally printed on
            a visible tag or label, otherwise leave it null.

            Categories (categoryId must be one of these ids, or null if none fit):
            {categories}

            Metadata fields (only include a key in "metadata" if its value is visually apparent;
            respect its declared type and allowed values):
            {fields}

            Respond with ONLY a single JSON object, no other text and no markdown fences, shaped
            exactly like this:
            {jsonShape}
            """;
    }

    public static ProductAiDraft ParseSuggestion(string body)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<RawInteractionResponse>(body, ResponseJson)
                ?? throw new ImageEditingException("The image analysis provider returned an unreadable response.");

            var text = parsed.OutputText?.Text;

            if (string.IsNullOrWhiteSpace(text) && parsed.Steps is not null)
            {
                text = parsed.Steps
                    .AsEnumerable()
                    .Reverse()
                    .SelectMany(step => step.Content ?? [])
                    .FirstOrDefault(item => item.Type == "text" && !string.IsNullOrWhiteSpace(item.Text))
                    ?.Text;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ImageEditingException("The image analysis provider returned no text.");
            }

            var json = StripCodeFence(text);

            var draft = JsonSerializer.Deserialize<ProductAiDraft>(json, ResponseJson)
                ?? throw new ImageEditingException("The image analysis provider returned an unreadable draft.");

            draft.Tags ??= [];

            return draft;
        }
        catch (ImageEditingException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            throw new ImageEditingException("The image analysis provider returned an unexpected response shape.", ex);
        }
    }

    /// <summary>Models frequently wrap JSON in a ```json ... ``` fence despite being asked not to.</summary>
    private static string StripCodeFence(string text)
    {
        var trimmed = text.Trim();

        if (!trimmed.StartsWith("```"))
        {
            return trimmed;
        }

        var firstNewline = trimmed.IndexOf('\n');
        var withoutOpenFence = firstNewline >= 0 ? trimmed[(firstNewline + 1)..] : trimmed;

        var closingFenceIndex = withoutOpenFence.LastIndexOf("```", StringComparison.Ordinal);

        return closingFenceIndex >= 0 ? withoutOpenFence[..closingFenceIndex].Trim() : withoutOpenFence.Trim();
    }

    private sealed class RawInteractionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public List<object> Input { get; set; } = [];
    }

    private sealed class RawImageInputPart
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "image";

        [JsonPropertyName("mime_type")]
        public string MimeType { get; set; } = string.Empty;

        [JsonPropertyName("data")]
        public string Data { get; set; } = string.Empty;
    }

    private sealed class RawTextInputPart
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "text";

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }

    private sealed class RawInteractionResponse
    {
        [JsonPropertyName("output_text")]
        public RawTextPart? OutputText { get; set; }

        [JsonPropertyName("steps")]
        public List<RawStep>? Steps { get; set; }
    }

    private sealed class RawStep
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("content")]
        public List<RawTextPart>? Content { get; set; }
    }

    private sealed class RawTextPart
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }
}
