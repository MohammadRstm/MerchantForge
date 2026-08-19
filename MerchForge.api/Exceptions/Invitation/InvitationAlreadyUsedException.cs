using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Invitation
{
    public class InvitationAlreadyUsedException : AppException
    {
        public InvitationAlreadyUsedException() : base(
            Enums.ErrorType.Conflict,
            "INVITATION_ALREADY_IN_USE",
            "Invitation already used"
        )
        {

        }

        
    }
}
