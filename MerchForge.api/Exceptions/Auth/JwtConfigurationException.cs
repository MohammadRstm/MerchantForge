using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Auth
{
    public class JwtConfigurationException : AppException
    {
        public JwtConfigurationException() : base(
            Enums.ErrorType.Authentication,
            "JWT_MISS_CONFIGURATION",
            "Jwt seceret key not configured") { }
    }
}
