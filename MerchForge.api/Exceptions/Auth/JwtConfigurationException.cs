using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Auth
{
    public class JwtConfigurationException : AppException
    {
        public JwtConfigurationException() : base("Jwt seceret key not configured") { }
    }
}
