namespace MerchForge.api.Services.Dashboard.interfaces
{
    public interface IWebsiteTemplateImageService
    {
        /// <summary>
        /// Validates and stores an uploaded template preview image, returning the
        /// relative URL to save on the template. Throws InvalidWebsiteTemplateImageException
        /// when the file is empty, too large, or not genuinely an image of an allowed type.
        /// </summary>
        Task<string> SaveAsync(
            IFormFile file,
            CancellationToken cancellationToken = default);
    }
}
