using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Auth
{
    public class InvalidCredentialsException : AppException
    {
        public InvalidCredentialsException() : base(
            Enums.ErrorType.Authentication,
            "INVALID_CREDENTIALS",
            "Invalid email or password")
        {
        }

    }
}
