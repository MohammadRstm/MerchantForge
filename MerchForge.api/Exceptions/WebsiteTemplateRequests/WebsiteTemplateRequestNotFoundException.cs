using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.WebsiteTemplateRequests
{
    public class WebsiteTemplateRequestNotFoundException : AppException
    {
        public WebsiteTemplateRequestNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "WEBSITE_TEMPLATE_REQUEST_NOT_FOUND",
            "Website template request not found")
        {
        }
    }
}
