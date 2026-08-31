using FluentValidation;
using MerchForge.api.DTOs.Auth;

namespace MerchForge.api.Validators.Auth
{
    public class RegisterSuperAdminRequestValidator : AbstractValidator<RegisterSuperAdminRequest>
    {
        public RegisterSuperAdminRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8);
        }
    }
}
