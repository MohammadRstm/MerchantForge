using FluentValidation;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Enums;

namespace MerchForge.api.Validators.Dashboard;

public class UpdateProductAttributeDefinitionRequestValidator : AbstractValidator<UpdateProductAttributeDefinitionRequest>
{
    public UpdateProductAttributeDefinitionRequestValidator()
    {
        RuleFor(x => x.Label)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.ValueType)
            .NotEmpty()
            .Must(v => Enum.TryParse<ProductAttributeValueType>(v, out _))
            .WithMessage("Value type must be one of Text, Number, Boolean, TextList, ColorList.");

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}
