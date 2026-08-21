using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;

namespace MerchForge.api.Services.BusinessDashboard.interfaces
{
    public interface IBusinessDashboardService
    {
        Task<BusinessDashboardStatsResponse> GetStatsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<PagedResult<BusinessProductResponse>> GetProductsAsync(
            Guid businessId,
            ProductsQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<List<BusinessMemberResponse>> GetMembersAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<BusinessSubscriptionResponse?> GetSubscriptionAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        // ---- product CRUD ----

        Task<ProductFormResponse> GetProductFormAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<BusinessProductDetailResponse> GetProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<BusinessProductDetailResponse> CreateProductAsync(
            Guid businessId,
            SaveProductRequest request,
            CancellationToken cancellationToken = default);

        Task<BusinessProductDetailResponse> UpdateProductAsync(
            Guid businessId,
            Guid productId,
            SaveProductRequest request,
            CancellationToken cancellationToken = default);

        Task DeleteProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default);

        // ---- website template ----

        Task<BusinessWebsiteTemplateStatusResponse> GetWebsiteTemplateStatusAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<ChosenWebsiteTemplateResponse> ChooseWebsiteTemplateAsync(
            Guid businessId,
            ChooseWebsiteTemplateRequest request,
            CancellationToken cancellationToken = default);
    }
}
