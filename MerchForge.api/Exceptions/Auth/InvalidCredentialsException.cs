using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Auth
{
    public class InvalidCredentialsException : AppException
    {
        public InvalidCredentialsException() : base("Invalid email or password")
        {
        }

    }
}
