using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Auth
{
    public class InvalidRefreshTokenException : AppException
    {
        public InvalidRefreshTokenException()
     : base(
           Enums.ErrorType.Authentication,
           "INVALID_REFRESH_TOKEN",
           "The refresh token is invalid or expired.")
        {
        }
    }
}
