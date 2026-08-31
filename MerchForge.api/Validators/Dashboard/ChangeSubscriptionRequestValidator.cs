using FluentValidation;
using MerchForge.api.DTOs.Dashboard;

namespace MerchForge.api.Validators.Dashboard;

public class ChangeSubscriptionRequestValidator : AbstractValidator<ChangeSubscriptionRequest>
{
    public ChangeSubscriptionRequestValidator()
    {
        RuleFor(x => x.SubscriptionPlanId)
            .NotEmpty();
    }
}
