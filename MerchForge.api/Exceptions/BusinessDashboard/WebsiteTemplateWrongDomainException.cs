using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard
{
    public class WebsiteTemplateWrongDomainException : AppException
    {
        public WebsiteTemplateWrongDomainException() : base(
            Enums.ErrorType.Validation,
            "WEBSITE_TEMPLATE_WRONG_DOMAIN",
            "That template does not belong to this business's domain")
        {
        }
    }
}
