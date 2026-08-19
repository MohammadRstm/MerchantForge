using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Storefront;

namespace MerchForge.api.Services.Storefront.interfaces
{
    /// <summary>
    /// Public storefront catalog reads.
    ///
    /// Every method takes the business context as a plain Guid rather than reading it
    /// from the request. When hostname-based resolution replaces the businessId query
    /// parameter, only the controller changes — this interface, the repository, and
    /// every SDK hook built on it stay as they are.
    /// </summary>
    public interface IStorefrontService
    {
        Task<StorefrontBusinessResponse> GetBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<List<StorefrontCategoryResponse>> GetCategoriesAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<PagedResult<StorefrontProductResponse>> GetProductsAsync(
            Guid businessId,
            StorefrontProductsQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<StorefrontProductDetailResponse> GetProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<List<StorefrontProductResponse>> GetRelatedProductsAsync(
            Guid businessId,
            Guid productId,
            int limit,
            CancellationToken cancellationToken = default);
    }
}
