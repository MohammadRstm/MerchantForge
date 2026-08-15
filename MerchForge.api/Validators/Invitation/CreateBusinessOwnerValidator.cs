using FluentValidation;
using MerchForge.api.DTOs.Invitations;

namespace MerchForge.api.Validators.Invitation
{
    public class CreateBusinessOwnerValidator : AbstractValidator<CreateBusinessOwnerInvitationRequest>
    {
        public CreateBusinessOwnerValidator() 
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);
        }
    }
}
