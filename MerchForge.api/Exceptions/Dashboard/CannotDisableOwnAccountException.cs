using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class CannotDisableOwnAccountException : AppException
    {
        public CannotDisableOwnAccountException() : base(
            Enums.ErrorType.Validation,
            "CANNOT_DISABLE_OWN_ACCOUNT",
            "You cannot disable your own account")
        {
        }
    }
}
