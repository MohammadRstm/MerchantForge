using System.Text.Json;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface IBusinessDashboardRepository
    {
        Task<(string Name, DateTime CreatedAt, string? WebsiteUrl)?> GetBusinessSummaryAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<int> CountMembersAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<int> CountProductsAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<int> CountProductDraftsAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<(decimal? Average, decimal? Min, decimal? Max)> GetProductPriceStatsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetProductsByCategoryAsync(Guid businessId, CancellationToken cancellationToken = default);

        /// <summary>Products with StockQuantity == 0 — untracked (null) inventory doesn't count as out of stock.</summary>
        Task<int> CountOutOfStockProductsAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<List<BusinessProductResponse>> GetRecentProductsAsync(
            Guid businessId,
            int take,
            CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetProductDraftsByStatusAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetMembersByRoleAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<List<DateTime>> GetProductCreationDatesSinceAsync(
            Guid businessId,
            DateTime since,
            CancellationToken cancellationToken = default);

        /// <summary>lowStockThreshold is only consulted when query.StockStatus is
        /// LowStock or InStock — pass the business's current threshold regardless,
        /// resolved once by the caller.</summary>
        Task<(List<BusinessProductResponse> Items, int TotalCount)> GetProductsAsync(
            Guid businessId,
            ProductsQueryRequest query,
            int lowStockThreshold,
            CancellationToken cancellationToken = default);

        Task<List<BusinessMemberResponse>> GetMembersAsync(Guid businessId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates the user and their membership together, so a failure on either
        /// cannot leave a user account with no business behind.
        /// </summary>
        Task CreateMemberAsync(
            User user,
            BusinessUser businessUser,
            CancellationToken cancellationToken = default);

        // ---- product CRUD ----

        Task<BusinessProductDetailResponse?> GetProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The business's metadata shape, plus the categories it may assign products
        /// to (its domain's platform categories and its own custom ones). Null when
        /// the business doesn't exist.
        /// </summary>
        Task<(JsonDocument? MetadataShape, List<ProductFormCategoryResponse> Categories)?> GetProductFormDataAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether this business may assign products to the given category — i.e. it
        /// belongs to the business's domain and is either a platform category or this
        /// business's own.
        /// </summary>
        Task<bool> CanUseCategoryAsync(
            Guid businessId,
            Guid categoryId,
            CancellationToken cancellationToken = default);

        Task<Product> CreateProductAsync(
            Product product,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Which business owns a product, or null when no product has that id.
        ///
        /// Unscoped on purpose. Every other product read here is filtered to one
        /// business; this one exists to answer the opposite question - whether an id
        /// supplied by a client is already spoken for by somebody else - which a
        /// scoped query cannot distinguish from the id simply not existing yet.
        /// </summary>
        Task<Guid?> GetProductOwnerBusinessIdAsync(
            Guid productId,
            CancellationToken cancellationToken = default);

        /// <summary>Loads a tracked product scoped to the business, for update/delete.</summary>
        Task<Product?> GetTrackedProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Replaces a product's entire image gallery and persists it, along with any
        /// other pending changes already made to the tracked product. product.Images
        /// must already be loaded (GetTrackedProductAsync does this) so the existing
        /// rows are known and can be removed.
        /// </summary>
        Task ReplaceProductImagesAsync(
            Product product,
            List<ProductImage> newImages,
            CancellationToken cancellationToken = default);

        Task DeleteProductAsync(
            Product product,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Every product in the catalog with its current- and previous-period sales
        /// (zero for products with no sales in a given window) plus a category
        /// roll-up — one bounded, catalog-sized query set the frontend derives every
        /// product-performance view (top products, revenue distribution, best
        /// sellers, needs-attention, zero-sales, category breakdown) from.
        /// </summary>
        Task<ProductPerformanceResponse> GetProductPerformanceAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        // ---- website template ----

        /// <summary>Null when the business doesn't exist. WebsiteTemplate* fields are null when no template has been chosen.</summary>
        Task<(Guid? BusinessDomainId, string? DomainName, Guid? WebsiteTemplateId, string? WebsiteTemplateName,
            string? WebsiteTemplateLabel, string? WebsiteTemplatePreviewImageUrl, DateTime? WebsiteTemplateChosenAt)?>
            GetBusinessWebsiteTemplateInfoAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<List<WebsiteTemplateOptionResponse>> GetActiveWebsiteTemplatesByDomainAsync(
            Guid businessDomainId,
            CancellationToken cancellationToken = default);

        /// <summary>Null unless an active template with this id exists in this domain — a business may only choose one of its own domain's templates.</summary>
        Task<WebsiteTemplateOptionResponse?> GetActiveWebsiteTemplateInDomainAsync(
            Guid websiteTemplateId,
            Guid businessDomainId,
            CancellationToken cancellationToken = default);

        // ---- inventory ----

        /// <summary>Null when the business doesn't exist.</summary>
        Task<int?> GetLowStockThresholdAsync(Guid businessId, CancellationToken cancellationToken = default);

        /// <summary>False when the business doesn't exist — the caller decides how to surface that.</summary>
        Task<bool> UpdateLowStockThresholdAsync(
            Guid businessId,
            int lowStockThreshold,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies the adjustment to an already-loaded tracked product (the caller is
        /// expected to have fetched it via GetTrackedProductAsync, same as
        /// UpdateProductAsync/DeleteProductAsync already do) and persists both the new
        /// StockQuantity and the ledger row in one SaveChangesAsync call. Returns null
        /// if the adjustment would take StockQuantity below zero — same
        /// signal-failure-via-return-value convention as
        /// FeatureCreditRepository.TryConsumeCreditAsync.
        /// </summary>
        Task<StockMovement?> AdjustStockAsync(
            Product product,
            int amount,
            string? reason,
            Guid createdByUserId,
            CancellationToken cancellationToken = default);

        Task<InventorySummaryResponse> GetInventorySummaryAsync(
            Guid businessId,
            int lowStockThreshold,
            CancellationToken cancellationToken = default);

        /// <summary>productId narrows to one product's activity (the product detail modal); null is the business-wide recent-activity feed.</summary>
        Task<List<StockMovementResponse>> GetRecentStockMovementsAsync(
            Guid businessId,
            int take,
            Guid? productId = null,
            CancellationToken cancellationToken = default);

        Task<InventoryAnalyticsResponse> GetInventoryAnalyticsAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default);

        Task<InventoryPerformanceResponse> GetInventoryPerformanceAsync(
            Guid businessId,
            DateTime from,
            DateTime to,
            int lowStockThreshold,
            CancellationToken cancellationToken = default);
    }
}
