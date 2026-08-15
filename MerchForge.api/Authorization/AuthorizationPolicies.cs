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

        // Features
        public const string Products = "Feature.Products";
        public const string Telegram = "Feature.Telegram";
        public const string WhatsApp = "Feature.WhatsApp";
        public const string AiProductGeneration = "Feature.AiProductGeneration";
    }
}
