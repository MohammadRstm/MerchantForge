namespace MerchForge.api.Configurations
{
    public class CustomerRefreshTokenOptions
    {
        public const string SectionName = "CustomerRefreshToken";

        public string CookieName { get; set; } = "customerRefreshToken";

        public string CookiePath { get; set; } = "/api/CustomerAuth";

        public bool Secure { get; set; } = true;

        public string SameSite { get; set; } = "Lax";

        public int ExpirationDays { get; set; } = 30;
    }
}
