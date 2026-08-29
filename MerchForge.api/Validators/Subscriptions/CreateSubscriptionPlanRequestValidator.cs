using FluentValidation;
using MerchForge.api.DTOs.Subscriptions;

namespace MerchForge.api.Validators.Subscriptions;

public class CreateSubscriptionPlanRequestValidator : AbstractValidator<CreateSubscriptionPlanRequest>
{
    public CreateSubscriptionPlanRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3);

        RuleFor(x => x.BillingInterval)
            .IsInEnum();

        RuleForEach(x => x.Features)
            .ChildRules(feature =>
            {
                feature.RuleFor(f => f.FeatureId).NotEmpty();
                feature.RuleFor(f => f.Limit).GreaterThan(0).When(f => f.Limit.HasValue);
            });
    }
}
