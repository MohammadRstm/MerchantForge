using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard
{
    public class WebsiteCustomizationDraftNotFoundException : AppException
    {
        public WebsiteCustomizationDraftNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "WEBSITE_CUSTOMIZATION_DRAFT_NOT_FOUND",
            "No customization draft exists yet for this business")
        {
        }
    }
}
