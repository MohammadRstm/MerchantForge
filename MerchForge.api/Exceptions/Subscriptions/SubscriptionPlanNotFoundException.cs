using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Subscriptions
{
    /// <summary>The referenced plan doesn't exist or is inactive.</summary>
    public class SubscriptionPlanNotFoundException : AppException
    {
        public SubscriptionPlanNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "SUBSCRIPTION_PLAN_NOT_FOUND",
            "That plan isn't available.")
        {
        }
    }
}
