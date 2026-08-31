using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Auth
{
    public class AccountDisabledException : AppException
    {
        public AccountDisabledException() : base(
            Enums.ErrorType.Authentication,
            "ACCOUNT_DISABLED",
            "This account has been disabled")
        {
        }
    }
}
