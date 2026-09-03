using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Storefront
{
    /// <summary>
    /// Raised when a review does not exist, or exists but belongs to a different
    /// business than the one making the request. Both cases return the same error for
    /// the same reason ProductNotFoundException does: an owner of business A must not
    /// be able to probe whether a given review id exists under business B.
    /// </summary>
    public class ProductReviewNotFoundException : AppException
    {
        public ProductReviewNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "PRODUCT_REVIEW_NOT_FOUND",
            "Review was not found")
        {
        }
    }
}
