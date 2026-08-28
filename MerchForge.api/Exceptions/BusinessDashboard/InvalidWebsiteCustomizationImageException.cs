using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard
{
    public class InvalidWebsiteCustomizationImageException : AppException
    {
        public InvalidWebsiteCustomizationImageException(string message) : base(
            Enums.ErrorType.Validation,
            "INVALID_WEBSITE_CUSTOMIZATION_IMAGE",
            message)
        {
        }
    }
}
