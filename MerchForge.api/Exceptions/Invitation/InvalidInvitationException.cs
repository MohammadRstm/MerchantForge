using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Invitation
{
    public class InvalidInvitationException : AppException
    {
        public InvalidInvitationException() : base(
           Enums.ErrorType.Conflict,
           "INVALID_INVITATION",
           "Invitation not found, might be because it's revoked or expired, or beause you are the wrong person to sign off on it")
        {

        }
    }
}
