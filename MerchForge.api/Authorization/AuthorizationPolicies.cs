namespace MerchForge.api.Authorization
{
    public static class AuthorizationPolicies
    {
        public const string SystemSuperAdmin = "SystemSuperAdmin"; 
        public const string SystemAdmin = "SystemAdmin";

        /* Business Roles */
        public const string BusinessMember = "BusinessMember";

        public const string BusinessAdmin = "BusinessAdmin";

        public const string BusinessOwner = "BusinessOwner";

        /* Customer (shopper) identity — structurally separate from every role above:
           this policy accepts only the "Customer" JWT scheme, so a customer token can
           never satisfy an owner/admin policy and vice versa. */
        public const string Customer = "Customer";

        // Features
        public const string Products = "Feature.Products";
        public const string Telegram = "Feature.Telegram";
        public const string WhatsApp = "Feature.WhatsApp";
        public const string AiProductGeneration = "Feature.AiProductGeneration";
        public const string AiImageEditing = "Feature.AiImageEditing";
        public const string WebsiteCustomizationBasic = "Feature.WebsiteCustomizationBasic";
        public const string WebsiteCustomizationAdvanced = "Feature.WebsiteCustomizationAdvanced";
    }
}
