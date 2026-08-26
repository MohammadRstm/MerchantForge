using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class WebsiteTemplateNotFoundException : AppException
    {
        public WebsiteTemplateNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "WEBSITE_TEMPLATE_NOT_FOUND",
            "Website template not found")
        {
        }
    }
}
