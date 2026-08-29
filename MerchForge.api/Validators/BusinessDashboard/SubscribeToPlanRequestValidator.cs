using FluentValidation;
using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.Validators.BusinessDashboard;

public class SubscribeToPlanRequestValidator : AbstractValidator<SubscribeToPlanRequest>
{
    public SubscribeToPlanRequestValidator()
    {
        RuleFor(x => x.SubscriptionPlanId)
            .NotEmpty();
    }
}
