using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class WebsiteTemplateCustomizableComponentKeyAlreadyExistsException : AppException
    {
        public WebsiteTemplateCustomizableComponentKeyAlreadyExistsException() : base(
            Enums.ErrorType.Conflict,
            "WEBSITE_TEMPLATE_CUSTOMIZABLE_COMPONENT_KEY_ALREADY_EXISTS",
            "A customizable component with this key already exists for this template")
        {
        }
    }
}
