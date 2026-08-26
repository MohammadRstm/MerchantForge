using MerchForge.api.Services.AI.Contracts;

namespace MerchForge.api.Services.AI.Interfaces;

/// <summary>
/// One call to a vision-capable model: one product image plus this business's field
/// schema in, a best-effort filled-in draft out. A separate boundary from
/// IProductImageEditingClient (returns pixels, not structured data) and from
/// IProductAiConversationClient (text-only - never actually sees the image).
/// </summary>
public interface IProductImageSuggestionClient
{
    string ModelName { get; }

    Task<ProductAiDraft> SuggestAsync(
        ImageEditInput image,
        ProductAiContext context,
        CancellationToken cancellationToken = default);
}
