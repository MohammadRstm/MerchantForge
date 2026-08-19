using MerchForge.api.Services.AI.Contracts;
using MerchForge.api.Services.AI.Interfaces;
using MerchForge.api.Exceptions.AI;

namespace MerchForge.api.Services.AI.Providers;

/// <summary>
/// Registered when no AI provider is configured.
///
/// Fails loudly on use rather than pretending to be an assistant: a stub that
/// invented plausible product data would let a half-real product reach the preview,
/// and the owner would have no way to tell. Everything around it — the draft
/// lifecycle, persistence, authorization, confirmation — still runs and is testable,
/// only the model call is absent.
/// </summary>
public class UnavailableProductAiConversationClient : IProductAiConversationClient
{
    public string ModelName => "unconfigured";

    public Task<ProductAiTurnResult> ContinueConversationAsync(
        ProductAiContext context,
        CancellationToken cancellationToken = default)
    {
        throw new AiConversationException(
            "AI product creation isn't configured on this server. You can still add the product manually.");
    }
}
