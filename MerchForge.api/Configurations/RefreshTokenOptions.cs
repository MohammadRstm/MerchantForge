namespace MerchForge.api.Configurations
{
    public class RefreshTokenOptions
    {
        public const string SectionName = "RefreshToken";

        public string CookieName { get; set; } = "refreshToken";

        public string CookiePath { get; set; } = "/api/Auth";

        public bool Secure { get; set; } = true;

        public string SameSite { get; set; } = "Lax";

        public int ExpirationDays { get; set; } = 30;
    }
}
