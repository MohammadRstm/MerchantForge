using FluentValidation;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Enums;

namespace MerchForge.api.Validators.Dashboard;

public class CreateProductAttributeDefinitionRequestValidator : AbstractValidator<CreateProductAttributeDefinitionRequest>
{
    public CreateProductAttributeDefinitionRequestValidator()
    {
        RuleFor(x => x.BusinessDomainId)
            .NotEmpty();

        // Lowercase-camelCase on purpose: this is the literal JSON key Product.Metadata
        // will carry it under, so it must be a safe, stable identifier.
        RuleFor(x => x.Key)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z][a-zA-Z0-9]*$")
            .WithMessage("Key must start lowercase and contain only letters and numbers, e.g. 'countryOfOrigin'.");

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
