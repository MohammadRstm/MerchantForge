using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Storefront;

namespace MerchForge.api.Services.ProductReviews.interfaces
{
    /// <summary>
    /// Product reviews for both the storefront and the owner's dashboard.
    ///
    /// Kept as one service rather than split across StorefrontService and
    /// BusinessDashboardService because the two halves share the same eligibility and
    /// visibility rules — separating them would mean duplicating those rules in two
    /// places and letting them drift.
    ///
    /// Like IStorefrontService, every method takes the business context as a plain
    /// Guid. For the storefront methods that is scoping; for the owner methods the
    /// caller's right to that business has already been established by the
    /// BusinessOwner policy before this is reached.
    /// </summary>
    public interface IProductReviewService
    {
        /// <summary>
        /// A product's visible reviews for public display. Throws
        /// ProductNotFoundException when the product doesn't exist under this business.
        /// </summary>
        Task<PagedResult<StorefrontProductReviewResponse>> GetVisibleReviewsAsync(
            Guid businessId,
            Guid productId,
            ProductReviewsQueryRequest query,
            CancellationToken cancellationToken = default);

        /// <summary>Average, count and per-star breakdown over visible reviews.</summary>
        Task<ProductReviewSummaryResponse> GetSummaryAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Whether this customer may review this product, plus their existing review if
        /// they have one — everything the storefront needs to choose what to render in
        /// place of the form, in a single request.
        /// </summary>
        Task<ProductReviewEligibilityResponse> GetEligibilityAsync(
            Guid businessId,
            Guid productId,
            Guid customerId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates the customer's review of this product, or updates it in place if
        /// they already have one — a customer has at most one review per product, so
        /// re-submitting is an edit rather than a second review.
        ///
        /// Throws ProductNotFoundException when the product isn't in this business, and
        /// ReviewRequiresPurchaseException when the customer hasn't ordered it.
        /// </summary>
        Task<MyProductReviewResponse> SubmitReviewAsync(
            Guid businessId,
            Guid productId,
            Guid customerId,
            CreateProductReviewRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>Every review on a product including hidden ones, for the owner.</summary>
        Task<PagedResult<OwnerProductReviewResponse>> GetReviewsForOwnerAsync(
            Guid businessId,
            Guid productId,
            ProductReviewsQueryRequest query,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Hides or unhides one review. Hiding removes it from the storefront and from
        /// the product's average but never deletes it. Throws
        /// ProductReviewNotFoundException when the review isn't in this business.
        /// </summary>
        Task SetReviewVisibilityAsync(
            Guid businessId,
            Guid reviewId,
            bool isHidden,
            CancellationToken cancellationToken = default);
    }
}
