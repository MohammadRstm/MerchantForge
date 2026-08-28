using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Subscriptions
{
    /// <summary>One or more FeatureId values in the request don't match any existing Feature.</summary>
    public class UnknownPlanFeatureException : AppException
    {
        public UnknownPlanFeatureException() : base(
            Enums.ErrorType.Validation,
            "UNKNOWN_PLAN_FEATURE",
            "One or more of the selected features don't exist.")
        {
        }
    }
}
