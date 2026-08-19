using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard
{
    public class InvalidProductImageException : AppException
    {
        public InvalidProductImageException(string message) : base(
            Enums.ErrorType.Validation,
            "INVALID_PRODUCT_IMAGE",
            message)
        {
        }
    }
}
