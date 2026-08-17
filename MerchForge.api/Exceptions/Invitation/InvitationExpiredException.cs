using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Invitation
{
    public class InvitationExpiredException : AppException
    {
        public InvitationExpiredException() : base(
            Enums.ErrorType.Conflict,
            "INVITATION_EXPIRED",
            "Invitation expired")
        {

        }
    }
}
