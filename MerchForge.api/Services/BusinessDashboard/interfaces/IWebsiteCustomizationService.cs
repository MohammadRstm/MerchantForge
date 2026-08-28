using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Dashboard;

namespace MerchForge.api.Services.BusinessDashboard.interfaces
{
    public interface IWebsiteCustomizationService
    {
        /// <summary>The active customizable-component catalogue for this business's current template. Empty when no template is chosen yet.</summary>
        Task<List<WebsiteTemplateCustomizableComponentResponse>> GetCatalogueAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        /// <summary>Creates the draft (as a snapshot of live Business data) the first time it's requested for this business.</summary>
        Task<WebsiteCustomizationDraftResponse> GetOrCreateDraftAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<WebsiteCustomizationDraftResponse> SaveDraftAsync(
            Guid businessId,
            SaveWebsiteCustomizationDraftRequest request,
            CancellationToken cancellationToken = default);

        Task<PublishWebsiteCustomizationResponse> PublishAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<RegeneratePreviewTokenResponse> RegeneratePreviewTokenAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);
    }
}
