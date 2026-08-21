using FluentValidation;
using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.Validators.BusinessDashboard;

public class ChooseWebsiteTemplateRequestValidator : AbstractValidator<ChooseWebsiteTemplateRequest>
{
    public ChooseWebsiteTemplateRequestValidator()
    {
        RuleFor(x => x.WebsiteTemplateId)
            .NotEmpty()
            .WithMessage("Select a template.");
    }
}
