using FluentValidation;
using MerchForge.api.DTOs.Subscriptions;

namespace MerchForge.api.Validators.Subscriptions;

public class PurchaseFeatureCreditsRequestValidator : AbstractValidator<PurchaseFeatureCreditsRequest>
{
    public PurchaseFeatureCreditsRequestValidator()
    {
        RuleFor(x => x.PackageId)
            .NotEmpty()
            .WithMessage("Select a package.");
    }
}
