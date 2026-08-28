using FluentValidation;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Enums;

namespace MerchForge.api.Validators.Dashboard;

public class CreateWebsiteTemplateCustomizableComponentRequestValidator
    : AbstractValidator<CreateWebsiteTemplateCustomizableComponentRequest>
{
    public CreateWebsiteTemplateCustomizableComponentRequestValidator()
    {
        RuleFor(x => x.WebsiteTemplateId)
            .NotEmpty();

        // Lowercase-camelCase on purpose: this is the literal JSON key
        // Business.WebsiteCustomizationValues will carry it under.
        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z][a-zA-Z0-9]*$")
            .WithMessage("Key must start lowercase and contain only letters and numbers, e.g. 'heroImage'.");

        RuleFor(x => x.Label)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ValueType)
            .NotEmpty()
            .Must(v => Enum.TryParse<WebsiteCustomizableValueType>(v, out _))
            .WithMessage("Value type must be one of Text, Textarea, Image, Color, Url, Boolean, Number, Select, Link.");

        RuleFor(x => x.HelpText)
            .MaximumLength(255);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}
