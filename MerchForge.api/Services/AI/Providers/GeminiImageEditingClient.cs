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
/// Google's Gemini image models via the Interactions API
/// (POST {baseUrl}interactions), the only class in MerchForge that knows this
/// provider exists.
///
/// Uses the HTTP API directly rather than an SDK, same reasoning as the OpenAI
/// client: the surface needed here is one call, and a direct request keeps the
/// request shape visible next to what it has to produce.
///
/// No retries on purpose. A failed edit surfaces to the owner, who can try again -
/// automatic retries on a paid, per-image API silently multiply cost.
/// </summary>
public class GeminiImageEditingClient : IProductImageEditingClient
{
    private static readonly JsonSerializerOptions ResponseJson = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly GeminiOptions _options;

    public GeminiImageEditingClient(HttpClient http, IOptions<GeminiOptions> options)
    {
        _options = options.Value;
        _http = http;

        _http.BaseAddress ??= new Uri(_options.BaseUrl);

        // Not a Bearer token - Gemini's REST API takes the key in its own header.
        _http.DefaultRequestHeaders.Remove("x-goog-api-key");
        _http.DefaultRequestHeaders.TryAddWithoutValidation("x-goog-api-key", _options.ApiKey);
    }

    public string ModelName => _options.ImageEditingModel;

    public async Task<ImageEditResult> EditAsync(
        List<ImageEditInput> images,
        string prompt,
        CancellationToken cancellationToken = default)
    {
        var input = new List<object>(images.Count + 1);

        // Images first, instruction last - matches the order shown in Google's own
        // examples, and gives the model every reference before the ask.
        input.AddRange(images.Select(image => new RawImageInputPart
        {
            MimeType = image.MimeType,
            Data = Convert.ToBase64String(image.Bytes),
        }));

        input.Add(new RawTextInputPart { Text = prompt });

        var payload = new RawInteractionRequest
        {
            Model = _options.ImageEditingModel,
            Input = input,
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
            // Status only, same reasoning as the OpenAI client: the body can echo
            // request content and this is logged, so it carries nothing else.
            throw new ImageEditingException(
                $"The image editing provider returned status {(int)response.StatusCode}.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return ParseResponse(body);
    }

    private static ImageEditResult ParseResponse(string body)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<RawInteractionResponse>(body, ResponseJson)
                ?? throw new ImageEditingException("The image editing provider returned an unreadable response.");

            // The convenience field, when present.
            var part = parsed.OutputImage;

            // Otherwise fall back to scanning the step content for an image - the
            // response can carry the result either way depending on how the model
            // chose to answer. Last step first, since that's the final output.
            if (part?.Data is null && parsed.Steps is not null)
            {
                part = parsed.Steps
                    .AsEnumerable()
                    .Reverse()
                    .SelectMany(step => step.Content ?? [])
                    .FirstOrDefault(item => item.Type == "image" && item.Data is not null);
            }

            if (part?.Data is null)
            {
                throw new ImageEditingException("The image editing provider returned no image.");
            }

            return new ImageEditResult(
                Convert.FromBase64String(part.Data),
                string.IsNullOrWhiteSpace(part.MimeType) ? "image/png" : part.MimeType);
        }
        catch (ImageEditingException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException or FormatException)
        {
            throw new ImageEditingException("The image editing provider returned an unexpected response shape.", ex);
        }
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
        [JsonPropertyName("output_image")]
        public RawImagePart? OutputImage { get; set; }

        [JsonPropertyName("steps")]
        public List<RawStep>? Steps { get; set; }
    }

    private sealed class RawStep
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("content")]
        public List<RawImagePart>? Content { get; set; }
    }

    private sealed class RawImagePart
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }

        [JsonPropertyName("mime_type")]
        public string? MimeType { get; set; }
    }
}
