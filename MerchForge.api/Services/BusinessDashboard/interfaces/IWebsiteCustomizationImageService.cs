using MerchForge.api.Enums;

namespace MerchForge.api.Services.BusinessDashboard.interfaces
{
    public interface IWebsiteCustomizationImageService
    {
        /// <summary>
        /// Validates and stores an uploaded logo/favicon/template image, returning the
        /// relative URL to include in a SaveWebsiteCustomizationDraftRequest. Throws
        /// InvalidWebsiteCustomizationImageException when the file is empty, too large
        /// for its kind, or not genuinely an image of an allowed type.
        /// </summary>
        Task<string> SaveAsync(
            Guid businessId,
            IFormFile file,
            WebsiteCustomizationImageKind kind,
            CancellationToken cancellationToken = default);
    }
}
