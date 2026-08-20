namespace MerchForge.api.Services.AI.Interfaces;

/// <summary>
/// Applies a natural-language modification to a product image ("make the background
/// neutral") and returns the URL of the result.
///
/// A separate boundary from the conversation client because image editing is a
/// different capability with a different provider surface, and because the agent's
/// job is to decide that an edit was requested, never to perform one.
/// </summary>
public interface IProductImageEditor
{
    /// <summary>
    /// False when no editing backend is configured, so the orchestration can tell the
    /// owner plainly instead of leaving the draft stuck waiting for an edit that will
    /// never arrive.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Returns the stored URL of the edited image. <paramref name="sourceImageUrl"/>
    /// is the relative URL previously returned by the image upload.
    /// </summary>
    Task<string> EditAsync(
        Guid businessId,
        string sourceImageUrl,
        string modificationPrompt,
        CancellationToken cancellationToken = default);
}
