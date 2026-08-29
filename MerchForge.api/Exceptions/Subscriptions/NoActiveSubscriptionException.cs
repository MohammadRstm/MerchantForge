using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Subscriptions
{
    /// <summary>The business has no Active subscription to cancel.</summary>
    public class NoActiveSubscriptionException : AppException
    {
        public NoActiveSubscriptionException() : base(
            Enums.ErrorType.NotFound,
            "NO_ACTIVE_SUBSCRIPTION",
            "This business doesn't have an active subscription.")
        {
        }
    }
}
