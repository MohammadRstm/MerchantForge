using MerchForge.api.DTOs.ProductAi;

namespace MerchForge.api.Services.ImageSuggestion.Interfaces;

public interface IImageSuggestionService
{
    /// <summary>
    /// Looks at one already-uploaded product image and returns a best-effort draft
    /// of this business's product fields — title, description, and anything else
    /// genuinely visible in the photo, with everything else left null. Stateless:
    /// no ProductDraft is created or persisted, this is a one-shot look, not a
    /// conversation. imageUrl is re-verified to belong to this business, nothing is
    /// trusted from the request alone.
    /// </summary>
    Task<ProductDraftProductResponse> SuggestAsync(
        Guid businessId,
        Guid userId,
        string imageUrl,
        CancellationToken cancellationToken = default);
}
