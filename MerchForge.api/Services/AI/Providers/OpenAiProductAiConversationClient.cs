using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.AI;
using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.AI.Interfaces;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Services.AI.Providers;

/// <summary>
/// OpenAI chat completions with strict structured outputs.
///
/// The only class in MerchForge that knows the provider exists. Uses the HTTP API
/// directly rather than an SDK: the surface needed here is one call, and a direct
/// request keeps the request shape visible next to the schema it has to satisfy.
///
/// No retries on purpose. A failed turn surfaces to the owner, who can send the
/// message again — automatic retries on a paid API silently multiply cost for a
/// conversation that is already interactive.
/// </summary>
public class OpenAiProductAiConversationClient : IProductAiConversationClient
{
    private static readonly JsonSerializerOptions ResponseJson = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly AiOptions _options;

    public OpenAiProductAiConversationClient(HttpClient http, IOptions<AiOptions> options)
    {
        _options = options.Value;
        _http = http;

        _http.BaseAddress ??= new Uri(_options.BaseUrl.TrimEnd('/') + "/");

        // Set here, never logged and never included in exception messages.
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);
    }

    public string ModelName => _options.ConversationModel;

    public async Task<ProductAiTurnResult> ContinueConversationAsync(
        ProductAiContext context,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _options.ConversationModel,
            messages = new object[]
            {
                new { role = "system", content = OpenAiPromptBuilder.BuildSystemInstructions() },
                new { role = "user", content = OpenAiPromptBuilder.BuildUserContent(context) },
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "product_ai_decision",
                    // Provider-enforced, so a malformed decision can't reach us.
                    strict = true,
                    schema = JsonDocument.Parse(OpenAiPromptBuilder.BuildDecisionSchema()).RootElement,
                },
            },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"),
        };

        using var response = await _http.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            // Status only. The body can echo request content, and this message is
            // logged — so it deliberately carries nothing but the code.
            throw new AiConversationException(
                $"The AI provider returned status {(int)response.StatusCode}.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return ParseCompletion(body, context);
    }

    private static ProductAiTurnResult ParseCompletion(string body, ProductAiContext context)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            var content = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new AiConversationException("The AI provider returned an empty decision.");
            }

            var raw = JsonSerializer.Deserialize<RawDecision>(content, ResponseJson)
                ?? throw new AiConversationException("The AI provider returned an unreadable decision.");

            var result = new ProductAiTurnResult
            {
                Decision = new ProductAiDecision
                {
                    Action = MapAction(raw.Action),
                    Message = raw.Message ?? string.Empty,
                    MissingFields = raw.MissingFields ?? [],
                    ImageModificationPrompt = raw.ImageModificationPrompt,
                    Draft = MapDraft(raw.Draft, context),
                },
            };

            if (root.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var prompt))
                {
                    result.PromptTokens = prompt.GetInt32();
                }

                if (usage.TryGetProperty("completion_tokens", out var completion))
                {
                    result.CompletionTokens = completion.GetInt32();
                }
            }

            return result;
        }
        catch (AiConversationException)
        {
            throw;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or IndexOutOfRangeException)
        {
            throw new AiConversationException("The AI provider returned an unexpected response shape.", ex);
        }
    }

    private static ProductAiAction MapAction(string? action) => action switch
    {
        "request_information" => ProductAiAction.RequestInformation,
        "update_draft" => ProductAiAction.UpdateDraft,
        "request_image_modification" => ProductAiAction.RequestImageModification,
        "ready_for_review" => ProductAiAction.ReadyForReview,
        "cancel" => ProductAiAction.Cancel,
        // The schema constrains this to the enum above, so anything else means the
        // contract drifted. Treated as "keep talking", which is the safe direction:
        // it can never skip ahead to proposing a product for review.
        _ => ProductAiAction.RequestInformation,
    };

    private static ProductAiDraft? MapDraft(RawDraft? raw, ProductAiContext context)
    {
        if (raw is null)
        {
            return null;
        }

        Guid? categoryId = Guid.TryParse(raw.CategoryId, out var parsed) ? parsed : null;

        return new ProductAiDraft
        {
            Title = raw.Title,
            Description = raw.Description,
            Price = raw.Price,
            CategoryId = categoryId,
            Metadata = MapMetadata(raw.Metadata, context),
        };
    }

    /// <summary>
    /// Turns the model's { key, values } list back into typed JSON.
    ///
    /// The model always sends strings, because the strict schema cannot express a
    /// per-key type. The declared type is already in the context, so the conversion
    /// happens here rather than being guessed downstream. A value that does not parse
    /// as its declared type is dropped rather than coerced into something plausible -
    /// downstream validation would reject it anyway, and inventing a number from
    /// "about twenty" would be worse than leaving the field unanswered.
    /// </summary>
    private static Dictionary<string, JsonElement>? MapMetadata(
        List<RawMetadataEntry>? entries,
        ProductAiContext context)
    {
        if (entries is null || entries.Count == 0)
        {
            return null;
        }

        var typesByKey = context.MetadataFields
            .ToDictionary(f => f.Key, f => f.ValueType, StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Key) || entry.Values is null)
            {
                continue;
            }

            var values = entry.Values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();

            if (values.Count == 0)
            {
                continue;
            }

            // Unknown keys are kept as text and rejected later by name, so the owner
            // is told which field was not recognised instead of it vanishing.
            var valueType = typesByKey.GetValueOrDefault(entry.Key, "Text");

            JsonElement? mapped = valueType switch
            {
                "TextList" => ToElement(values),
                "Number" => decimal.TryParse(values[0], out var number) ? ToElement(number) : null,
                "Boolean" => bool.TryParse(values[0], out var flag) ? ToElement(flag) : null,
                _ => ToElement(values[0]),
            };

            if (mapped is { } element)
            {
                result[entry.Key] = element;
            }
        }

        return result.Count == 0 ? null : result;
    }

    private static JsonElement ToElement<T>(T value) =>
        JsonSerializer.SerializeToDocument(value).RootElement.Clone();

    private sealed class RawDecision
    {
        public string? Action { get; set; }
        public string? Message { get; set; }
        public List<string>? MissingFields { get; set; }
        public string? ImageModificationPrompt { get; set; }
        public RawDraft? Draft { get; set; }
    }

    private sealed class RawMetadataEntry
    {
        public string? Key { get; set; }
        public List<string>? Values { get; set; }
    }

    private sealed class RawDraft
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? CategoryId { get; set; }
        public List<RawMetadataEntry>? Metadata { get; set; }
    }
}
