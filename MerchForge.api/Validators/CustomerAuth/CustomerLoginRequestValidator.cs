using FluentValidation;
using MerchForge.api.DTOs.CustomerAuth;

namespace MerchForge.api.Validators.CustomerAuth
{
    public class CustomerLoginRequestValidator : AbstractValidator<CustomerLoginRequest>
    {
        public CustomerLoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8);

            RuleFor(x => x.ReturnUrl)
                .Must(BeAValidAbsoluteUrl)
                .When(x => !string.IsNullOrWhiteSpace(x.ReturnUrl))
                .WithMessage("ReturnUrl must be an absolute URL");
        }

        private static bool BeAValidAbsoluteUrl(string? url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }
}
