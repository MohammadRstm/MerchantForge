using FluentValidation;
using MerchForge.api.DTOs.CustomerAuth;

namespace MerchForge.api.Validators.CustomerAuth
{
    public class UpdateCustomerProfileRequestValidator : AbstractValidator<UpdateCustomerProfileRequest>
    {
        public UpdateCustomerProfileRequestValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.Phone)
                .MaximumLength(50);

            RuleFor(x => x.AddressLine1)
                .MaximumLength(255);

            RuleFor(x => x.AddressLine2)
                .MaximumLength(255);

            RuleFor(x => x.City)
                .MaximumLength(100);

            RuleFor(x => x.State)
                .MaximumLength(100);

            RuleFor(x => x.PostalCode)
                .MaximumLength(20);

            RuleFor(x => x.Country)
                .MaximumLength(100);
        }
    }
}
