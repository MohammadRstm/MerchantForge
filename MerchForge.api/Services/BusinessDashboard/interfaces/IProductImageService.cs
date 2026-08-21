namespace MerchForge.api.Services.BusinessDashboard.interfaces
{
    public interface IProductImageService
    {
        /// <summary>
        /// Validates and stores an uploaded product image, returning the relative URL
        /// to save on the product. Throws InvalidProductImageException when the file
        /// is empty, too large, or not genuinely an image of an allowed type.
        /// </summary>
        Task<string> SaveAsync(
            Guid businessId,
            IFormFile file,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Same validation and storage as the upload path, for bytes that didn't come
        /// through a form upload - an AI-edited image, say. contentType is trusted
        /// here rather than re-derived, since the caller already knows what it asked
        /// the provider to return; the byte-signature check still applies.
        /// </summary>
        Task<string> SaveAsync(
            Guid businessId,
            byte[] bytes,
            string contentType,
            CancellationToken cancellationToken = default);
    }
}
