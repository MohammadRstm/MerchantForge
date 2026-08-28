using FluentValidation;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Enums;

namespace MerchForge.api.Validators.Dashboard;

public class UpdateWebsiteTemplateCustomizableComponentRequestValidator
    : AbstractValidator<UpdateWebsiteTemplateCustomizableComponentRequest>
{
    public UpdateWebsiteTemplateCustomizableComponentRequestValidator()
    {
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
