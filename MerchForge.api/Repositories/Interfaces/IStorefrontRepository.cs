using MerchForge.api.DTOs.Storefront;

namespace MerchForge.api.Repositories.Interfaces
{
    /// <summary>
    /// Read-side data access for the public storefront API.
    ///
    /// Every method takes businessId and every query filters on it. businessId is an
    /// identification mechanism, not an authorization one — these endpoints are
    /// public — so the guarantee this interface provides is scoping, not access
    /// control: business A's storefront can never be served business B's data.
    /// </summary>
    public interface IStorefrontRepository
    {
        Task<StorefrontBusinessResponse?> GetBusinessAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<bool> BusinessExistsAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<List<StorefrontCategoryResponse>> GetCategoriesAsync(
            Guid businessId,
            CancellationToken cancellationToken = default);

        Task<(List<StorefrontProductResponse> Items, int TotalCount)> GetProductsAsync(
            Guid businessId,
            StorefrontProductsQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<StorefrontProductDetailResponse?> GetProductAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default);

        Task<bool> ProductExistsAsync(
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
