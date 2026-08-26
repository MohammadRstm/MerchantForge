using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.WebsiteTemplateRequests
{
    public class WebsiteTemplateRequestAlreadyOpenException : AppException
    {
        public WebsiteTemplateRequestAlreadyOpenException() : base(
            Enums.ErrorType.Conflict,
            "WEBSITE_TEMPLATE_REQUEST_ALREADY_OPEN",
            "This business already has an open website request")
        {
        }
    }
}
