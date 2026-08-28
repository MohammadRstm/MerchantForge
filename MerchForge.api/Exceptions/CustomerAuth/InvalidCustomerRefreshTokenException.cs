using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.CustomerAuth
{
    public class InvalidCustomerRefreshTokenException : AppException
    {
        public InvalidCustomerRefreshTokenException() : base(
            Enums.ErrorType.Authentication,
            "INVALID_CUSTOMER_REFRESH_TOKEN",
            "Invalid or expired session")
        {
        }
    }
}
