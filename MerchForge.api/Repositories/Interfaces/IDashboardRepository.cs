using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Enums;
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

        /// <summary>Platform-wide, excludes Cancelled orders.</summary>
        Task<int> CountOrdersAsync(CancellationToken cancellationToken = default);

        Task<int> CountBusinessesCreatedSinceAsync(DateTime since, CancellationToken cancellationToken = default);

        /// <summary>Recorded order totals grouped by currency — see CurrencyTotalResponse's own doc comment for why.</summary>
        Task<List<CurrencyTotalResponse>> GetRecordedOrderRevenueByCurrencyAsync(CancellationToken cancellationToken = default);

        Task<List<DashboardBusinessResponse>> GetRecentBusinessesAsync(int take, CancellationToken cancellationToken = default);

        Task<List<DateTime>> GetBusinessCreationDatesSinceAsync(DateTime since, CancellationToken cancellationToken = default);

        Task<List<DateTime>> GetProductCreationDatesSinceAsync(DateTime since, CancellationToken cancellationToken = default);

        Task<(List<DashboardUserResponse> Items, int TotalCount)> GetUsersAsync(
            UsersQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Null when no user with this id exists.</summary>
        Task<DashboardUserDetailResponse?> GetUserDetailAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>Loads a tracked entity for a disable/enable mutation.</summary>
        Task<User?> GetTrackedUserAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<(List<DashboardBusinessResponse> Items, int TotalCount)> GetBusinessesAsync(
            BusinessesQueryRequest query,
            CancellationToken cancellationToken = default);

        /// <summary>Owner, domain, and website-template navigation properties are loaded for the business-detail view. Null when the business doesn't exist.</summary>
        Task<Business?> GetBusinessDetailCoreAsync(Guid businessId, CancellationToken cancellationToken = default);

        /// <summary>Loads a tracked entity for the metadata-shape mutation.</summary>
        Task<Business?> GetTrackedBusinessAsync(Guid businessId, CancellationToken cancellationToken = default);

        /// <summary>The active fields a business's domain allows products to carry — what a metadata-shape edit may choose keys from.</summary>
        Task<List<ProductAttributeDefinition>> GetActiveAttributeDefinitionsForDomainAsync(
            Guid businessDomainId,
            CancellationToken cancellationToken = default);

        // ---- product attribute definition CRUD (domain field catalogue) ----

        /// <summary>All definitions (active and inactive), optionally filtered to one domain, with BusinessDomain loaded.</summary>
        Task<List<ProductAttributeDefinition>> GetAttributeDefinitionsAsync(
            Guid? businessDomainId,
            CancellationToken cancellationToken = default);

        Task<bool> AttributeDefinitionKeyExistsAsync(
            Guid businessDomainId,
            string key,
            CancellationToken cancellationToken = default);

        Task CreateAttributeDefinitionAsync(
            ProductAttributeDefinition definition,
            CancellationToken cancellationToken = default);

        /// <summary>Loads a tracked entity for an update/activate mutation.</summary>
        Task<ProductAttributeDefinition?> GetTrackedAttributeDefinitionAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        /// <summary>Loads with BusinessDomain, for building a response after a mutation.</summary>
        Task<ProductAttributeDefinition?> GetAttributeDefinitionWithDomainAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        // ---- website template customizable component CRUD (per-template capability catalogue) ----

        /// <summary>All active components for a template, ordered by DisplayOrder — the shape of that template's customization form.</summary>
        Task<List<WebsiteTemplateCustomizableComponent>> GetActiveCustomizableComponentsForTemplateAsync(
            Guid websiteTemplateId,
            CancellationToken cancellationToken = default);

        /// <summary>All components (active and inactive), optionally filtered to one template, with WebsiteTemplate loaded.</summary>
        Task<List<WebsiteTemplateCustomizableComponent>> GetCustomizableComponentsAsync(
            Guid? websiteTemplateId,
            CancellationToken cancellationToken = default);

        Task<bool> CustomizableComponentKeyExistsAsync(
            Guid websiteTemplateId,
            string key,
            CancellationToken cancellationToken = default);

        Task CreateCustomizableComponentAsync(
            WebsiteTemplateCustomizableComponent component,
            CancellationToken cancellationToken = default);

        /// <summary>Loads a tracked entity for an update/activate mutation.</summary>
        Task<WebsiteTemplateCustomizableComponent?> GetTrackedCustomizableComponentAsync(
            Guid id,
            CancellationToken cancellationToken = default);

        /// <summary>Loads with WebsiteTemplate, for building a response after a mutation.</summary>
        Task<WebsiteTemplateCustomizableComponent?> GetCustomizableComponentWithTemplateAsync(
            Guid id,
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

        // ---- customers ----

        Task<(List<DashboardCustomerResponse> Items, int TotalCount)> GetCustomersAsync(
            CustomersQueryRequest query,
            CancellationToken cancellationToken = default);

        /// <summary>Null when no customer with this id exists.</summary>
        Task<DashboardCustomerDetailResponse?> GetCustomerDetailAsync(
            Guid customerId,
            CancellationToken cancellationToken = default);

        /// <summary>Loads a tracked entity for a profile-edit mutation.</summary>
        Task<Customer?> GetTrackedCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

        Task<CustomerStatsResponse> GetCustomerStatsAsync(
            int newCustomersPeriodDays, CancellationToken cancellationToken = default);

        /// <summary>Customer.CreatedAt dates since the given time — fed into TimeSeriesBuilder by the service, same pattern as GetBusinessCreationDatesSinceAsync.</summary>
        Task<List<DateTime>> GetCustomerCreationDatesSinceAsync(DateTime since, CancellationToken cancellationToken = default);

        Task<List<TopCustomerResponse>> GetTopCustomersAsync(
            TopCustomersRankBy rankBy, string currency, int take, CancellationToken cancellationToken = default);

        /// <summary>Distinct customers per business (a global customer counts once per business they've ordered from, not once platform-wide) - see the response's own framing on the frontend.</summary>
        Task<List<KeyCountResponse>> GetCustomerDistributionByBusinessAsync(CancellationToken cancellationToken = default);

        Task<List<DashboardCustomerResponse>> GetRecentCustomersAsync(int take, CancellationToken cancellationToken = default);

        Task<List<BusinessOptionResponse>> GetBusinessOptionsAsync(CancellationToken cancellationToken = default);

        Task<(List<CustomerOrderResponse> Items, int TotalCount)> GetCustomerOrdersAsync(
            Guid customerId, Guid? businessId, int page, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>Monthly recorded spend for one customer, one row per (month, currency) they ordered in.</summary>
        Task<List<CustomerSpendPointResponse>> GetCustomerSpendOverTimeAsync(
            Guid customerId, CancellationToken cancellationToken = default);

        // ---- subscriptions (platform-wide, Subscriptions tab) ----

        Task<(List<AdminSubscriptionListItemResponse> Items, int TotalCount)> GetSubscriptionsAsync(
            SubscriptionsQueryRequest query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The most recent `take` Subscription rows platform-wide (a row is only
        /// created on a business's first subscription or a plan switch — a renewal
        /// advances the existing row's period in place, so it never appears here).
        /// </summary>
        Task<List<RecentSubscriptionActivityEntryResponse>> GetRecentSubscriptionActivityAsync(
            int take,
            CancellationToken cancellationToken = default);
    }
}
