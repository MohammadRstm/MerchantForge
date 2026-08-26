using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class InvalidWebsiteTemplateVideoException : AppException
    {
        public InvalidWebsiteTemplateVideoException(string message) : base(
            Enums.ErrorType.Validation,
            "INVALID_WEBSITE_TEMPLATE_VIDEO",
            message)
        {
        }
    }
}
