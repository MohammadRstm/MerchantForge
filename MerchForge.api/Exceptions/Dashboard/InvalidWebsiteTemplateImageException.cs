using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class InvalidWebsiteTemplateImageException : AppException
    {
        public InvalidWebsiteTemplateImageException(string message) : base(
            Enums.ErrorType.Validation,
            "INVALID_WEBSITE_TEMPLATE_IMAGE",
            message)
        {
        }
    }
}
