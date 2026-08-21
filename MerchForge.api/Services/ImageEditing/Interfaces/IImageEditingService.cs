using MerchForge.api.DTOs.ImageEditing;

namespace MerchForge.api.Services.ImageEditing.Interfaces;

public interface IImageEditingService
{
    /// <summary>
    /// Edits one or more images together against a single instruction ("put both
    /// products on the same white background") and returns the result. Synchronous -
    /// there's no conversation to resume, so the whole call either lands as a
    /// Completed job or throws, and a Failed job is recorded either way for audit.
    /// </summary>
    Task<ImageEditJobResponse> EditAsync(
        Guid businessId,
        Guid userId,
        List<IFormFile> images,
        string prompt,
        CancellationToken cancellationToken = default);

    Task<ImageEditJobResponse> GetAsync(
        Guid businessId,
        Guid jobId,
        CancellationToken cancellationToken = default);
}
