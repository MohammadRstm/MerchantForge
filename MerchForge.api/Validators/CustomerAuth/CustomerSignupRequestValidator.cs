using FluentValidation;
using MerchForge.api.DTOs.CustomerAuth;

namespace MerchForge.api.Validators.CustomerAuth
{
    public class CustomerSignupRequestValidator : AbstractValidator<CustomerSignupRequest>
    {
        public CustomerSignupRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8);

            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.ReturnUrl)
                .Must(BeAValidAbsoluteUrl)
                .When(x => !string.IsNullOrWhiteSpace(x.ReturnUrl))
                .WithMessage("ReturnUrl must be an absolute URL");

            RuleFor(x => x.AgreedToTerms)
                .Equal(true)
                .WithMessage("You must agree to the Terms of Service and Privacy Policy to create an account.");
        }

        private static bool BeAValidAbsoluteUrl(string? url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out _);
        }
    }
}
