using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.WebsiteTemplateRequests
{
    /// <summary>Thrown by Start Build on a request that isn't Pending, and by Close Request on a request that's already Closed — this is what prevents accidental duplicate closure.</summary>
    public class WebsiteTemplateRequestInvalidStatusTransitionException : AppException
    {
        public WebsiteTemplateRequestInvalidStatusTransitionException() : base(
            Enums.ErrorType.Conflict,
            "WEBSITE_TEMPLATE_REQUEST_INVALID_STATUS_TRANSITION",
            "This request can't move to that status from its current status")
        {
        }
    }
}
