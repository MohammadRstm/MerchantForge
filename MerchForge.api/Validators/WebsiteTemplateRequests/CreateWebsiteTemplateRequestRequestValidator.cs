using FluentValidation;
using MerchForge.api.DTOs.WebsiteTemplateRequests;

namespace MerchForge.api.Validators.WebsiteTemplateRequests;

public class CreateWebsiteTemplateRequestRequestValidator : AbstractValidator<CreateWebsiteTemplateRequestRequest>
{
    public CreateWebsiteTemplateRequestRequestValidator()
    {
        RuleFor(x => x.WebsiteTemplateId)
            .NotEmpty()
            .WithMessage("Select a template.");

        RuleFor(x => x.CustomizationNotes)
            .NotEmpty()
            .WithMessage("Tell us what you'd like to change.")
            .MaximumLength(4000)
            .WithMessage("Keep it under 4000 characters.");
    }
}
