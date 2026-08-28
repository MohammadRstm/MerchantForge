using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.CustomerAuth
{
    public class CustomerEmailAlreadyExistsException : AppException
    {
        public CustomerEmailAlreadyExistsException() : base(
            Enums.ErrorType.Conflict,
            "CUSTOMER_EMAIL_ALREADY_EXISTS",
            "An account with this email already exists")
        {
        }
    }
}
