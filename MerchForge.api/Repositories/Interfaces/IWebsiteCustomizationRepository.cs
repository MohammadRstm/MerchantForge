using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface IWebsiteCustomizationRepository
    {
        /// <summary>Loads a tracked Business for reading/prefilling a new draft, or for Publish's copy target.</summary>
        Task<Business?> GetTrackedBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<BusinessWebsiteDraft?> GetTrackedDraftAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task CreateDraftAsync(
            BusinessWebsiteDraft draft,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
