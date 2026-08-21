using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class WebsiteTemplateNameAlreadyExistsException : AppException
    {
        public WebsiteTemplateNameAlreadyExistsException() : base(
            Enums.ErrorType.Conflict,
            "WEBSITE_TEMPLATE_NAME_ALREADY_EXISTS",
            "A website template with that name already exists")
        {
        }
    }
}
