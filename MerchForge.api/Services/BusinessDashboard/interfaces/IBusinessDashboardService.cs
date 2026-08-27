using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.WebsiteTemplateRequests;

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

        // ---- website template requests ----

        Task<WebsiteTemplateOptionsResponse> GetWebsiteTemplateOptionsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<WebsiteTemplateRequestResponse> CreateWebsiteTemplateRequestAsync(
            Guid businessId,
            Guid requestedByUserId,
            CreateWebsiteTemplateRequestRequest request,
            CancellationToken cancellationToken = default);

        Task<List<WebsiteTemplateRequestResponse>> GetWebsiteTemplateRequestsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        // ---- inventory ----

        Task<StockAdjustmentResponse> AdjustStockAsync(
            Guid businessId,
            Guid productId,
            StockAdjustmentRequest request,
            Guid actingUserId,
            CancellationToken cancellationToken = default);

        Task<InventorySummaryResponse> GetInventorySummaryAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<List<StockMovementResponse>> GetRecentStockMovementsAsync(
            Guid businessId,
            int take,
            CancellationToken cancellationToken = default);

        Task UpdateLowStockThresholdAsync(
            Guid businessId,
            int lowStockThreshold,
            CancellationToken cancellationToken = default);
    }
}
