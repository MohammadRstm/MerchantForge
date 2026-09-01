using FluentValidation;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Validators.Common;

namespace MerchForge.api.Validators.Dashboard;

public class SubscriptionsQueryRequestValidator : PagedQueryValidator<SubscriptionsQueryRequest>
{
    public SubscriptionsQueryRequestValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(255);

        RuleFor(x => x.PlanName)
            .MaximumLength(100);

        RuleFor(x => x.SortBy)
            .IsInEnum();

        RuleFor(x => x.BillingInterval)
            .IsInEnum()
            .When(x => x.BillingInterval.HasValue);

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);
    }
}
