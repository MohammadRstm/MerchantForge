namespace MerchForge.api.Services.Dashboard.interfaces
{
    public interface IWebsiteTemplateVideoService
    {
        /// <summary>
        /// Validates and stores an uploaded template preview video, returning the
        /// relative URL to save on the template. Throws InvalidWebsiteTemplateVideoException
        /// when the file is empty, too large, or not genuinely a video of an allowed type.
        /// </summary>
        Task<string> SaveAsync(
            IFormFile file,
            CancellationToken cancellationToken = default);
    }
}
