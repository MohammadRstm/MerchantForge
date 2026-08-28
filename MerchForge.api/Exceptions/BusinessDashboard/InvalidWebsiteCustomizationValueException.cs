using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard
{
    public class InvalidWebsiteCustomizationValueException : AppException
    {
        public InvalidWebsiteCustomizationValueException(string message) : base(
            Enums.ErrorType.Validation,
            "INVALID_WEBSITE_CUSTOMIZATION_VALUE",
            message)
        {
        }
    }
}
