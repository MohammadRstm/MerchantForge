namespace MerchForge.api.Configurations
{
    /// <summary>
    /// The customer JWT scheme reuses the platform's existing Jwt:SecretKey/Issuer (see
    /// JwtOptions) — only the audience and lifetime are customer-specific. Reusing the
    /// secret is what the plan calls out as acceptable ("same secret/issuer is fine"); it
    /// is the distinct scheme name ("Customer") plus this distinct Audience, not a
    /// distinct secret, that keeps a customer token from being accepted by any
    /// owner/admin policy and vice versa.
    /// </summary>
    public class CustomerJwtOptions
    {
        public const string SectionName = "Jwt:Customer";

        public string Audience { get; set; } = string.Empty;

        public int AccessTokenExpirationMinutes { get; set; } = 15;
    }
}
