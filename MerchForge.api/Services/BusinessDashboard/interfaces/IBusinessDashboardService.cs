using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.WebsiteTemplateRequests;
using MerchForge.api.Enums;

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

        /// <summary>
        /// Subscribes (or switches) a business to a plan: no real payment gateway
        /// exists yet, so this always succeeds and takes effect immediately. Any
        /// existing Active subscription is cancelled and replaced outright - no
        /// proration, no cancel-first step. Grants the plan's initial
        /// ai.image_editing credit allotment synchronously so it's available right
        /// away rather than waiting for the next renewal job pass.
        /// </summary>
        Task<BusinessSubscriptionResponse> SubscribeToPlanAsync(
            Guid businessId,
            Guid subscriptionPlanId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Marks the business's Active subscription to end at CurrentPeriodEnd
        /// instead of renewing - access continues uninterrupted until then.
        /// Subscribing to any plan afterward replaces it and clears this.
        /// </summary>
        Task<BusinessSubscriptionResponse> CancelSubscriptionAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<List<SubscriptionHistoryEntryResponse>> GetSubscriptionHistoryAsync(
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

        Task<ProductCatalogOverviewResponse> GetProductCatalogOverviewAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<ProductAnalyticsResponse> GetProductAnalyticsAsync(
            Guid businessId,
            ProductAnalyticsQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<ProductPerformanceResponse> GetProductPerformanceAsync(
            Guid businessId,
            ProductAnalyticsQueryRequest query,
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
            Guid? productId = null,
            CancellationToken cancellationToken = default);

        Task UpdateLowStockThresholdAsync(
            Guid businessId,
            int lowStockThreshold,
            CancellationToken cancellationToken = default);

        Task<InventoryAnalyticsResponse> GetInventoryAnalyticsAsync(
            Guid businessId,
            InventoryAnalyticsQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<InventoryPerformanceResponse> GetInventoryPerformanceAsync(
            Guid businessId,
            InventoryAnalyticsQueryRequest query,
            CancellationToken cancellationToken = default);

        // ---- orders ----

        Task<PagedResult<BusinessOrderResponse>> GetOrdersAsync(
            Guid businessId,
            OrdersQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<BusinessOrderDetailResponse> GetOrderAsync(
            Guid businessId,
            Guid orderId,
            CancellationToken cancellationToken = default);

        Task<BusinessOrderDetailResponse> UpdateOrderStatusAsync(
            Guid businessId,
            Guid orderId,
            OrderStatus status,
            Guid changedByUserId,
            CancellationToken cancellationToken = default);

        Task<BusinessOrderDetailResponse> UpdateOrderPaymentStatusAsync(
            Guid businessId,
            Guid orderId,
            PaymentStatus paymentStatus,
            CancellationToken cancellationToken = default);

        Task<OrderStatsResponse> GetOrderStatsAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<List<OrderNoteResponse>> GetOrderNotesAsync(Guid businessId, Guid orderId, CancellationToken cancellationToken = default);

        Task<OrderNoteResponse> AddOrderNoteAsync(
            Guid businessId,
            Guid orderId,
            string content,
            Guid createdByUserId,
            CancellationToken cancellationToken = default);

        Task<List<OrderStatusHistoryEntryResponse>> GetOrderStatusHistoryAsync(Guid businessId, Guid orderId, CancellationToken cancellationToken = default);

        Task<OrderAnalyticsResponse> GetOrderAnalyticsAsync(
            Guid businessId,
            OrderAnalyticsQueryRequest query,
            CancellationToken cancellationToken = default);
    }
}
