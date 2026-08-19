using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class CannotRevokeOwnSessionException : AppException
    {
        public CannotRevokeOwnSessionException() : base(
            Enums.ErrorType.Validation,
            "CANNOT_REVOKE_OWN_SESSION",
            "You cannot revoke your own sessions")
        {

        }
    }
}
