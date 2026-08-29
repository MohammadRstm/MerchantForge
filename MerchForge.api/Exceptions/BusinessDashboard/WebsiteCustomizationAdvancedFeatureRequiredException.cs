using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard
{
    public class WebsiteCustomizationAdvancedFeatureRequiredException : AppException
    {
        public WebsiteCustomizationAdvancedFeatureRequiredException() : base(
            Enums.ErrorType.Authorization,
            "WEBSITE_CUSTOMIZATION_ADVANCED_FEATURE_REQUIRED",
            "Your current plan does not include advanced website customization (social links, business hours, template fields). Upgrade to change these fields.")
        {
        }
    }
}
