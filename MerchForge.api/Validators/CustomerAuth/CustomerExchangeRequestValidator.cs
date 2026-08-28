using FluentValidation;
using MerchForge.api.DTOs.CustomerAuth;

namespace MerchForge.api.Validators.CustomerAuth
{
    public class CustomerExchangeRequestValidator : AbstractValidator<CustomerExchangeRequest>
    {
        public CustomerExchangeRequestValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.ReturnUrl)
                .NotEmpty()
                .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                .WithMessage("ReturnUrl must be an absolute URL");
        }
    }
}
