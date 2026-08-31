using FluentValidation;
using MerchForge.api.DTOs.Auth;

namespace MerchForge.api.Validators.Auth
{
    public class CompleteBusinessMemberRegistrationRequestValidator : AbstractValidator<CompleteBusinessMemberRegistrationRequest>
    {
        public CompleteBusinessMemberRegistrationRequestValidator()
        {
            RuleFor(x => x.InvitationToken)
                .NotEmpty();

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8);
        }
    }
}
