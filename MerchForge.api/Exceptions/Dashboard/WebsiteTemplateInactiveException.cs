using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class WebsiteTemplateInactiveException : AppException
    {
        public WebsiteTemplateInactiveException() : base(
            Enums.ErrorType.Conflict,
            "WEBSITE_TEMPLATE_INACTIVE",
            "This website template is not active and cannot be showcased yet")
        {
        }
    }
}
