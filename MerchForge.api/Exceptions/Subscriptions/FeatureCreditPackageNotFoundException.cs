using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Subscriptions
{
    /// <summary>The referenced package doesn't exist, is inactive, or its feature no longer supports credit purchase.</summary>
    public class FeatureCreditPackageNotFoundException : AppException
    {
        public FeatureCreditPackageNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "FEATURE_CREDIT_PACKAGE_NOT_FOUND",
            "That package isn't available.")
        {
        }
    }
}
