using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Auth
{
    public class InvalidRefreshTokenException : AppException
    {
        public InvalidRefreshTokenException()
     : base("The refresh token is invalid or expired.")
        {
        }
    }
}
