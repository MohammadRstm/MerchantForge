using MerchForge.api.Services.AI.Contracts;

namespace MerchForge.api.Services.AI.Interfaces;

/// <summary>
/// The single boundary to the AI provider. Everything provider-specific — SDK types,
/// model names, prompt assembly, structured-output schemas, retries — lives behind
/// this and nowhere else, so swapping providers touches one class.
///
/// Intentionally stateless: conversation state belongs to ProductDraft in our
/// database, not to a provider-side thread we cannot query, migrate or recover.
/// </summary>
public interface IProductAiConversationClient
{
    /// <summary>Provider/model identifier, for logging.</summary>
    string ModelName { get; }

    /// <summary>
    /// Runs one turn. Throws <see cref="Exceptions.AI.AiConversationException"/> if
    /// the provider fails or returns something that doesn't match the decision
    /// schema — callers get a decision or an exception, never a half-parsed guess.
    /// </summary>
    Task<ProductAiTurnResult> ContinueConversationAsync(
        ProductAiContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>The decision plus the provider metadata worth logging.</summary>
public class ProductAiTurnResult
{
    public ProductAiDecision Decision { get; set; } = new();

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }
}
