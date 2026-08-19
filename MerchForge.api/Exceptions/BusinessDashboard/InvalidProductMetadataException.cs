using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard
{
    public class InvalidProductMetadataException : AppException
    {
        public InvalidProductMetadataException(string message) : base(
            Enums.ErrorType.Validation,
            "INVALID_PRODUCT_METADATA",
            message)
        {
        }
    }
}
