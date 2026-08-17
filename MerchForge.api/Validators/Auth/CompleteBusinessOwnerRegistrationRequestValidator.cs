using FluentValidation;
using MerchForge.api.DTOs.Auth;

namespace MerchForge.api.Validators.Auth
{
    public class CompleteBusinessOwnerRegistrationRequestValidator : AbstractValidator<CompleteBusinessOwnerRegistrationRequest>
    {
        public CompleteBusinessOwnerRegistrationRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.BusinessName)
                .NotEmpty()
                .MaximumLength(255);
        }
    }
}
