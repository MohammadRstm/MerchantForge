using MerchForge.api.Services.AI.Interfaces;

namespace MerchForge.api.Services.AI.Providers;

/// <summary>
/// No image editing backend. Reports itself unavailable rather than throwing, so the
/// orchestration tells the owner it will keep the original image and the conversation
/// carries on — an edit request is a nice-to-have, not a blocker.
/// </summary>
public class UnavailableProductImageEditor : IProductImageEditor
{
    public bool IsAvailable => false;

    public Task<string> EditAsync(
        Guid businessId,
        string sourceImageUrl,
        string modificationPrompt,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("No image editing provider is configured.");
    }
}
