using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.CustomerAuth
{
    public class InvalidCustomerCredentialsException : AppException
    {
        public InvalidCustomerCredentialsException() : base(
            Enums.ErrorType.Authentication,
            "INVALID_CUSTOMER_CREDENTIALS",
            "Invalid email or password")
        {
        }
    }
}
