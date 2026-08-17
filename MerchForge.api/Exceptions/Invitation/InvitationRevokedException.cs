using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Invitation
{
    public class InvitationRevokedException : AppException
    {
        public InvitationRevokedException() : base(
            Enums.ErrorType.Conflict,
            "INVITATION_REVOKED",
            "Your invitation has been revoked"
        )
        {

        }
    }
}
