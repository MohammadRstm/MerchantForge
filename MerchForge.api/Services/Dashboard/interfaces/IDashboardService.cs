using MerchForge.api.DTOs.Audit;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.DTOs.WebsiteTemplateRequests;
using MerchForge.api.Enums;

namespace MerchForge.api.Services.Dashboard.interfaces
{
    public interface IDashboardService
    {
        Task<DashboardStatsResponse> GetPlatformStatsAsync(CancellationToken cancellationToken = default);

        Task<PagedResult<DashboardUserResponse>> GetUsersAsync(
            UsersQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<DashboardUserDetailResponse> GetUserDetailAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task<RevokeUserSessionsResponse> RevokeUserSessionsAsync(
            Guid targetUserId,
            Guid actingUserId,
            CancellationToken cancellationToken = default);

        Task<DashboardUserDetailResponse> DisableUserAsync(
            Guid targetUserId,
            Guid actingUserId,
            CancellationToken cancellationToken = default);

        Task<DashboardUserDetailResponse> EnableUserAsync(
            Guid targetUserId,
            Guid actingUserId,
            CancellationToken cancellationToken = default);

        Task<RevokeUserSessionsResponse> RevokeAllSessionsAsync(
            Guid actingUserId,
            CancellationToken cancellationToken = default);

        // ---- audit / security ----

        Task<PagedResult<AuditLogResponse>> GetAuditLogsAsync(
            AuditLogQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<SecurityOverviewResponse> GetSecurityOverviewAsync(CancellationToken cancellationToken = default);

        Task<FailedLoginStatsResponse> GetFailedLoginStatsAsync(CancellationToken cancellationToken = default);

        Task<List<SecurityAlertResponse>> GetSecurityAlertsAsync(CancellationToken cancellationToken = default);

        Task<PagedResult<DashboardBusinessResponse>> GetBusinessesAsync(
            BusinessesQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<BusinessDetailResponse> GetBusinessDetailAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a SuperAdmin-triggered showcase business for a domain: a real
        /// business + owner login, wired to the domain's active template, subscribed to
        /// Pro Yearly, and seeded with a realistic product/customer/order history. See
        /// Business.IsDemo's doc comment for why this exists and what it's excluded
        /// from. At most one per domain.
        /// </summary>
        Task<DemoBusinessResponse> CreateDemoBusinessAsync(
            CreateDemoBusinessRequest request,
            CancellationToken cancellationToken = default);

        Task<RevokeUserSessionsResponse> RevokeBusinessSessionsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        // ---- business analytics (reuses the same repository methods the Owner Dashboard calls) ----

        Task<OrderAnalyticsResponse> GetBusinessOrderAnalyticsAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<List<BusinessOrderResponse>> GetBusinessRecentOrdersAsync(
            Guid businessId,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<InventorySummaryResponse> GetBusinessInventorySummaryAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<ProductPerformanceResponse> GetBusinessProductPerformanceAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<CustomerSnapshotResponse> GetBusinessCustomerSnapshotAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<List<ProductFormFieldResponse>> GetBusinessMetadataShapeAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<List<ProductFormFieldResponse>> UpdateBusinessMetadataShapeAsync(
            Guid businessId,
            UpdateMetadataShapeRequest request,
            CancellationToken cancellationToken = default);

        // ---- product attribute definitions (domain field catalogue) ----

        Task<List<ProductAttributeDefinitionResponse>> GetAttributeDefinitionsAsync(
            Guid? businessDomainId,
            CancellationToken cancellationToken = default);

        Task<ProductAttributeDefinitionResponse> CreateAttributeDefinitionAsync(
            CreateProductAttributeDefinitionRequest request,
            CancellationToken cancellationToken = default);

        Task<ProductAttributeDefinitionResponse> UpdateAttributeDefinitionAsync(
            Guid id,
            UpdateProductAttributeDefinitionRequest request,
            CancellationToken cancellationToken = default);

        Task<ProductAttributeDefinitionResponse> SetAttributeDefinitionActiveAsync(
            Guid id,
            bool isActive,
            CancellationToken cancellationToken = default);

        // ---- website template customizable components (per-template capability catalogue) ----

        Task<List<WebsiteTemplateCustomizableComponentResponse>> GetCustomizableComponentsAsync(
            Guid? websiteTemplateId,
            CancellationToken cancellationToken = default);

        Task<WebsiteTemplateCustomizableComponentResponse> CreateCustomizableComponentAsync(
            CreateWebsiteTemplateCustomizableComponentRequest request,
            CancellationToken cancellationToken = default);

        Task<WebsiteTemplateCustomizableComponentResponse> UpdateCustomizableComponentAsync(
            Guid id,
            UpdateWebsiteTemplateCustomizableComponentRequest request,
            CancellationToken cancellationToken = default);

        Task<WebsiteTemplateCustomizableComponentResponse> SetCustomizableComponentActiveAsync(
            Guid id,
            bool isActive,
            CancellationToken cancellationToken = default);

        // ---- website templates ----

        Task<PagedResult<WebsiteTemplateResponse>> GetWebsiteTemplatesAsync(
            WebsiteTemplatesQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<TemplateStatsResponse> GetTemplateStatsAsync(CancellationToken cancellationToken = default);

        Task<List<DomainTemplateSummaryResponse>> GetDomainTemplateSummaryAsync(CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetRequestedTemplatesAsync(int take, CancellationToken cancellationToken = default);

        Task<List<TimeSeriesPointResponse>> GetTemplateRequestTrendAsync(int days, CancellationToken cancellationToken = default);

        Task<WebsiteTemplateResponse> CreateWebsiteTemplateAsync(
            CreateWebsiteTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<string> UploadWebsiteTemplateImageAsync(
            IFormFile file,
            CancellationToken cancellationToken = default);

        Task<WebsiteTemplateDetailResponse> GetWebsiteTemplateDetailAsync(
            Guid websiteTemplateId,
            CancellationToken cancellationToken = default);

        Task<WebsiteTemplateResponse> UpdateWebsiteTemplateAsync(
            Guid websiteTemplateId,
            UpdateWebsiteTemplateRequest request,
            CancellationToken cancellationToken = default);

        Task<WebsiteTemplateResponse> DeactivateWebsiteTemplateAsync(
            Guid websiteTemplateId,
            CancellationToken cancellationToken = default);

        Task<WebsiteTemplateResponse> ReactivateWebsiteTemplateAsync(
            Guid websiteTemplateId,
            CancellationToken cancellationToken = default);

        // ---- website template requests ----

        Task<PagedResult<WebsiteTemplateRequestSummaryResponse>> GetWebsiteTemplateRequestsAsync(
            WebsiteTemplateRequestsQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<WebsiteTemplateRequestDetailResponse> GetWebsiteTemplateRequestAsync(
            Guid websiteTemplateRequestId,
            CancellationToken cancellationToken = default);

        Task<WebsiteTemplateRequestDetailResponse> StartWebsiteTemplateRequestBuildAsync(
            Guid websiteTemplateRequestId,
            CancellationToken cancellationToken = default);

        // ---- customers ----

        Task<PagedResult<DashboardCustomerResponse>> GetCustomersAsync(
            CustomersQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<DashboardCustomerDetailResponse> GetCustomerDetailAsync(
            Guid customerId,
            CancellationToken cancellationToken = default);

        Task<DashboardCustomerDetailResponse> UpdateCustomerAsync(
            Guid customerId,
            UpdateCustomerRequest request,
            CancellationToken cancellationToken = default);

        Task<RevokeCustomerSessionsResponse> RevokeCustomerSessionsAsync(
            Guid customerId,
            CancellationToken cancellationToken = default);

        Task<CustomerStatsResponse> GetCustomerStatsAsync(
            int newCustomersPeriodDays,
            CancellationToken cancellationToken = default);

        Task<List<TimeSeriesPointResponse>> GetCustomerGrowthAsync(
            int days,
            CancellationToken cancellationToken = default);

        Task<List<TopCustomerResponse>> GetTopCustomersAsync(
            TopCustomersRankBy rankBy,
            string? currency,
            int take,
            CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetCustomerDistributionByBusinessAsync(
            CancellationToken cancellationToken = default);

        Task<List<DashboardCustomerResponse>> GetRecentCustomersAsync(
            int take,
            CancellationToken cancellationToken = default);

        Task<List<BusinessOptionResponse>> GetBusinessOptionsAsync(
            CancellationToken cancellationToken = default);

        Task<PagedResult<CustomerOrderResponse>> GetCustomerOrdersAsync(
            Guid customerId,
            Guid? businessId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<List<CustomerSpendPointResponse>> GetCustomerSpendOverTimeAsync(
            Guid customerId,
            CancellationToken cancellationToken = default);

        Task<WebsiteTemplateRequestDetailResponse> CloseWebsiteTemplateRequestAsync(
            Guid websiteTemplateRequestId,
            Guid closedByUserId,
            CloseWebsiteTemplateRequestRequest request,
            CancellationToken cancellationToken = default);

        // ---- subscriptions (platform-wide, Subscriptions tab) ----

        Task<PagedResult<AdminSubscriptionListItemResponse>> GetSubscriptionsAsync(
            SubscriptionsQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<List<RecentSubscriptionActivityEntryResponse>> GetRecentSubscriptionActivityAsync(
            int take,
            CancellationToken cancellationToken = default);

        Task<List<SubscriptionHistoryEntryResponse>> GetBusinessSubscriptionHistoryAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<BusinessSubscriptionResponse> ChangeBusinessSubscriptionAsync(
            Guid businessId,
            Guid subscriptionPlanId,
            CancellationToken cancellationToken = default);

        Task<BusinessSubscriptionResponse> CancelBusinessSubscriptionAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);
    }
}
