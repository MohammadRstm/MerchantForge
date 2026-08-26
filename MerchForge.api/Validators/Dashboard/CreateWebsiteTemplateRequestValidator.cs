using FluentValidation;
using MerchForge.api.DTOs.Dashboard;

namespace MerchForge.api.Validators.Dashboard;

public class CreateWebsiteTemplateRequestValidator : AbstractValidator<CreateWebsiteTemplateRequest>
{
    public CreateWebsiteTemplateRequestValidator()
    {
        RuleFor(x => x.BusinessDomainId)
            .NotEmpty()
            .WithMessage("Select a domain.");

        // Lowercase-hyphen-numeric on purpose: this is expected to match a physical
        // template project's own folder name (e.g. "fashion-template-02"), which a
        // later deployment step will look up literally.
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$")
            .WithMessage("Name must be lowercase letters, numbers and hyphens only, e.g. 'fashion-template-02'.");

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
