using System.Text.Json;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface IBusinessDashboardRepository
    {
        Task<(string Name, DateTime CreatedAt)?> GetBusinessSummaryAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<int> CountMembersAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<int> CountProductsAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<int> CountProductDraftsAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<(decimal? Average, decimal? Min, decimal? Max)> GetProductPriceStatsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetProductsByCategoryAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetProductDraftsByStatusAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<List<KeyCountResponse>> GetMembersByRoleAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<List<DateTime>> GetProductCreationDatesSinceAsync(
            Guid businessId,
            DateTime since,
            CancellationToken cancellationToken = default);

        Task<(List<BusinessProductResponse> Items, int TotalCount)> GetProductsAsync(
            Guid businessId,
            ProductsQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<List<BusinessMemberResponse>> GetMembersAsync(Guid businessId, CancellationToken cancellationToken = default);

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

        /// <summary>Loads a tracked product scoped to the business, for update/delete.</summary>
        Task<Product?> GetTrackedProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);

        Task DeleteProductAsync(
            Product product,
            CancellationToken cancellationToken = default);
    }
}
