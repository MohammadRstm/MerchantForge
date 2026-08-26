using FluentValidation;
using MerchForge.api.DTOs.Dashboard;

namespace MerchForge.api.Validators.Dashboard;

public class UpdateWebsiteTemplateRequestValidator : AbstractValidator<UpdateWebsiteTemplateRequest>
{
    public UpdateWebsiteTemplateRequestValidator()
    {
        RuleFor(x => x.Label)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.VideoPreviewUrl)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.PreviewWebsiteUrl)
            .MaximumLength(500);

        RuleFor(x => x.DisplayOrder)
            .GreaterThanOrEqualTo(0);
    }
}
