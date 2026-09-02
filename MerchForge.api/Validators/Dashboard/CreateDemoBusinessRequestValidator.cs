using FluentValidation;
using MerchForge.api.DTOs.Dashboard;

namespace MerchForge.api.Validators.Dashboard
{
    public class CreateDemoBusinessRequestValidator : AbstractValidator<CreateDemoBusinessRequest>
    {
        public CreateDemoBusinessRequestValidator()
        {
            RuleFor(x => x.WebsiteTemplateId)
                .NotEmpty()
                .WithMessage("Select a website template.");

            RuleFor(x => x.BusinessName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.OwnerFirstName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.OwnerLastName)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.OwnerEmail)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(255);

            RuleFor(x => x.OwnerPassword)
                .NotEmpty()
                .MinimumLength(8);
        }
    }
}
