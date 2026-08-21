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

        // ---- website template ----

        /// <summary>Null when the business doesn't exist. WebsiteTemplate* fields are null when no template has been chosen.</summary>
        Task<(Guid? BusinessDomainId, string? DomainName, Guid? WebsiteTemplateId, string? WebsiteTemplateName,
            string? WebsiteTemplateLabel, string? WebsiteTemplateVideoPreviewUrl, DateTime? WebsiteTemplateChosenAt)?>
            GetBusinessWebsiteTemplateInfoAsync(Guid businessId, CancellationToken cancellationToken = default);

        Task<List<WebsiteTemplateOptionResponse>> GetActiveWebsiteTemplatesByDomainAsync(
            Guid businessDomainId,
            CancellationToken cancellationToken = default);

        /// <summary>Null unless an active template with this id exists in this domain — a business may only choose one of its own domain's templates.</summary>
        Task<WebsiteTemplateOptionResponse?> GetActiveWebsiteTemplateInDomainAsync(
            Guid websiteTemplateId,
            Guid businessDomainId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the business's chosen template, but only if it doesn't already have
        /// one -- returns false (no-op) on a business that already chose, rather than
        /// overwriting it, so a duplicate request can never silently switch templates.
        /// </summary>
        Task<bool> ChooseWebsiteTemplateAsync(
            Guid businessId,
            Guid websiteTemplateId,
            CancellationToken cancellationToken = default);
    }
}
