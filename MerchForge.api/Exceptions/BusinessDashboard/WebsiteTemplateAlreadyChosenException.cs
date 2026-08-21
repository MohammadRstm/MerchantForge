using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard
{
    public class WebsiteTemplateAlreadyChosenException : AppException
    {
        public WebsiteTemplateAlreadyChosenException() : base(
            Enums.ErrorType.Conflict,
            "WEBSITE_TEMPLATE_ALREADY_CHOSEN",
            "This business has already chosen a website template")
        {
        }
    }
}
