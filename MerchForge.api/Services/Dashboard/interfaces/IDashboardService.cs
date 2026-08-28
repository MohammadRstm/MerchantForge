using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.DTOs.WebsiteTemplateRequests;

namespace MerchForge.api.Services.Dashboard.interfaces
{
    public interface IDashboardService
    {
        Task<DashboardStatsResponse> GetPlatformStatsAsync(CancellationToken cancellationToken = default);

        Task<PagedResult<DashboardUserResponse>> GetUsersAsync(
            UsersQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<RevokeUserSessionsResponse> RevokeUserSessionsAsync(
            Guid targetUserId,
            Guid actingUserId,
            CancellationToken cancellationToken = default);

        Task<PagedResult<DashboardBusinessResponse>> GetBusinessesAsync(
            BusinessesQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<BusinessDetailResponse> GetBusinessDetailAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<RevokeUserSessionsResponse> RevokeBusinessSessionsAsync(
            Guid businessId,
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

        // ---- website templates ----

        Task<List<WebsiteTemplateResponse>> GetWebsiteTemplatesAsync(CancellationToken cancellationToken = default);

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

        Task<WebsiteTemplateRequestDetailResponse> CloseWebsiteTemplateRequestAsync(
            Guid websiteTemplateRequestId,
            Guid closedByUserId,
            CloseWebsiteTemplateRequestRequest request,
            CancellationToken cancellationToken = default);
    }
}
