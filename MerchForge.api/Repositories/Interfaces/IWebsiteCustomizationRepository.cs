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

        /// <summary>
        /// Copies the draft onto the live Business row in one transaction, re-reading
        /// both the draft and the current template's catalogue inside that transaction
        /// (not from a request-scoped read taken before it started) so a concurrent
        /// draft-save can't be silently clobbered or read stale. Template-field keys no
        /// longer in the current template's active catalogue are dropped rather than
        /// failing the whole publish -- returned so the caller can surface a warning.
        /// Throws WebsiteCustomizationDraftNotFoundException if no draft exists yet.
        /// </summary>
        Task<(List<string> DroppedTemplateFieldKeys, DateTime PublishedAt)> PublishAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);
    }
}
