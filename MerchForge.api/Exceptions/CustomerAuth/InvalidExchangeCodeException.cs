using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.CustomerAuth
{
    public class InvalidExchangeCodeException : AppException
    {
        public InvalidExchangeCodeException() : base(
            Enums.ErrorType.Authentication,
            "INVALID_EXCHANGE_CODE",
            "Invalid, expired, or already-used exchange code")
        {
        }
    }
}
