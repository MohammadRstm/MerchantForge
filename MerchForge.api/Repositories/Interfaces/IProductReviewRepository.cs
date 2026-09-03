using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    /// <summary>
    /// Data access for product reviews, serving three callers with different rights:
    /// the public storefront (visible reviews only), the reviewing customer (their own
    /// review, whatever its visibility), and the business owner (everything for their
    /// own business, including hidden).
    ///
    /// Every method takes businessId and filters on it. For the storefront that is
    /// scoping rather than authorization, exactly as in IStorefrontRepository; for the
    /// owner methods the caller's right to that businessId has already been settled by
    /// the BusinessOwner policy before this is reached, and the filter here is the
    /// second line of defence rather than the first.
    /// </summary>
    public interface IProductReviewRepository
    {
        /// <summary>
        /// One page of a product's visible reviews, newest first. Hidden reviews are
        /// excluded here rather than by the caller, so no storefront path can leak one
        /// by forgetting to filter.
        /// </summary>
        Task<(List<StorefrontProductReviewResponse> Items, int TotalCount)> GetVisibleReviewsAsync(
            Guid businessId,
            Guid productId,
            ProductReviewsQueryRequest query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Average, count and per-star breakdown over a product's visible reviews.
        /// Returns an empty summary (null average, zero count, all-zero breakdown) for
        /// a product with none.
        /// </summary>
        Task<ProductReviewSummaryResponse> GetSummaryAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// One page of every review on a product including hidden ones, for the owner's
        /// moderation view.
        /// </summary>
        Task<(List<OwnerProductReviewResponse> Items, int TotalCount)> GetReviewsForOwnerAsync(
            Guid businessId,
            Guid productId,
            ProductReviewsQueryRequest query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The tracked entity for a customer's own review of a product, or null. Used
        /// both to answer "have I already reviewed this" and as the upsert's read half.
        /// </summary>
        Task<ProductReview?> GetByProductAndCustomerAsync(
            Guid businessId,
            Guid productId,
            Guid customerId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether this customer has at least one non-cancelled order with this
        /// business containing this product — the verified-purchase test. Guest orders
        /// have no CustomerId and so can never satisfy it.
        /// </summary>
        Task<bool> HasPurchasedProductAsync(
            Guid businessId,
            Guid productId,
            Guid customerId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The tracked review for an owner-side visibility change, scoped to the
        /// owner's own business. Null when it doesn't exist or belongs to another
        /// business — the caller cannot tell which.
        /// </summary>
        Task<ProductReview?> GetForOwnerAsync(
            Guid businessId,
            Guid reviewId,
            CancellationToken cancellationToken = default);

        Task AddAsync(ProductReview review, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
