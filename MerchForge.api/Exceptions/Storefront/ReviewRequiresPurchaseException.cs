using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Storefront
{
    /// <summary>
    /// Raised when a customer tries to review a product they have not bought from this
    /// business. Eligibility is "has at least one order that isn't Cancelled containing
    /// this product" — the same loose definition of a real order that the dashboard's
    /// own analytics use. Payment status is deliberately not part of it, because there
    /// is no payment gateway yet and PaymentStatus is effectively always Pending.
    ///
    /// A guest order can never satisfy this: it has no CustomerId, and matching on the
    /// snapshotted email instead would be a weaker identity rule than this codebase
    /// uses anywhere else.
    /// </summary>
    public class ReviewRequiresPurchaseException : AppException
    {
        public ReviewRequiresPurchaseException() : base(
            Enums.ErrorType.Conflict,
            "REVIEW_REQUIRES_PURCHASE",
            "Only customers who have ordered this product can review it")
        {
        }
    }
}
