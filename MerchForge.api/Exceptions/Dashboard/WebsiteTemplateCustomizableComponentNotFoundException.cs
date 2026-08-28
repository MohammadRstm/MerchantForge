using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class WebsiteTemplateCustomizableComponentNotFoundException : AppException
    {
        public WebsiteTemplateCustomizableComponentNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "WEBSITE_TEMPLATE_CUSTOMIZABLE_COMPONENT_NOT_FOUND",
            "Customizable component was not found")
        {
        }
    }
}
