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
    }
}
