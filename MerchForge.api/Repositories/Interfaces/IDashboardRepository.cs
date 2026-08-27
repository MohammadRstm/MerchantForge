using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface IDashboardRepository
    {
        Task<int> CountUsersAsync(CancellationToken cancellationToken = default);

        Task<int> CountBusinessesAsync(CancellationToken cancellationToken = default);

        Task<int> CountProductsAsync(CancellationToken cancellationToken = default);

        Task<int> CountProductDraftsAsync(CancellationToken cancellationToken = default);

        Task<int> CountPendingInvitationsAsync(CancellationToken cancellationToken = default);

        /// <summary>Pending/InProgress vs Closed counts across every website template request platform-wide.</summary>
        Task<(int Pending, int Completed)> GetWebsiteTemplateRequestStatusCountsAsync(CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetUserCountsBySystemRoleAsync(CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetBusinessUserCountsByRoleAsync(CancellationToken cancellationToken = default);

        /// <summary>Businesses grouped by domain name; businesses with no domain set are grouped under "Unassigned".</summary>
        Task<List<KeyCountResponse>> GetBusinessCountsByDomainAsync(CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetSubscriptionStatusCountsAsync(CancellationToken cancellationToken = default);

        Task<int> CountActiveSessionsAsync(CancellationToken cancellationToken = default);

        Task<List<DashboardBusinessResponse>> GetRecentBusinessesAsync(int take, CancellationToken cancellationToken = default);

        Task<List<DateTime>> GetBusinessCreationDatesSinceAsync(DateTime since, CancellationToken cancellationToken = default);

        Task<List<DateTime>> GetProductCreationDatesSinceAsync(DateTime since, CancellationToken cancellationToken = default);

        Task<(List<DashboardUserResponse> Items, int TotalCount)> GetUsersAsync(
            UsersQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<(List<DashboardBusinessResponse> Items, int TotalCount)> GetBusinessesAsync(
            BusinessesQueryRequest query,
            CancellationToken cancellationToken = default);

        /// <summary>Owner, domain, and website-template navigation properties are loaded for the business-detail view. Null when the business doesn't exist.</summary>
        Task<Business?> GetBusinessDetailCoreAsync(Guid businessId, CancellationToken cancellationToken = default);

        /// <summary>Loads a tracked entity for the metadata-shape mutation.</summary>
        Task<Business?> GetTrackedBusinessAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<List<BusinessFeatureCreditResponse>> GetBusinessFeatureCreditsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        /// <summary>The active fields a business's domain allows products to carry — what a metadata-shape edit may choose keys from.</summary>
        Task<List<ProductAttributeDefinition>> GetActiveAttributeDefinitionsForDomainAsync(
            Guid businessDomainId,
            CancellationToken cancellationToken = default);

        // ---- website templates ----

        Task<List<WebsiteTemplateResponse>> GetWebsiteTemplatesAsync(CancellationToken cancellationToken = default);

        Task<bool> WebsiteTemplateNameExistsAsync(string name, CancellationToken cancellationToken = default);

        Task<WebsiteTemplate> CreateWebsiteTemplateAsync(
            WebsiteTemplate template,
            CancellationToken cancellationToken = default);

        Task<WebsiteTemplateDetailResponse?> GetWebsiteTemplateDetailAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        /// <summary>Loads a tracked entity for an update/deactivate mutation.</summary>
        Task<WebsiteTemplate?> GetTrackedWebsiteTemplateAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
