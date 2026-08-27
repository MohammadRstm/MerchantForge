using FluentValidation;
using MerchForge.api.DTOs.Dashboard;

namespace MerchForge.api.Validators.Dashboard;

public class UpdateMetadataShapeRequestValidator : AbstractValidator<UpdateMetadataShapeRequest>
{
    public UpdateMetadataShapeRequestValidator()
    {
        RuleForEach(x => x.Fields).SetValidator(new UpdateMetadataShapeFieldRequestValidator());
    }
}

public class UpdateMetadataShapeFieldRequestValidator : AbstractValidator<UpdateMetadataShapeFieldRequest>
{
    public UpdateMetadataShapeFieldRequestValidator()
    {
        RuleFor(x => x.Key)
            .NotEmpty();

        RuleFor(x => x.Label)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.ValueType)
            .NotEmpty();

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}
