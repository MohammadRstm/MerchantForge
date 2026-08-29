using FluentValidation;
using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.Validators.BusinessDashboard;

public class OrderAnalyticsQueryRequestValidator : AbstractValidator<OrderAnalyticsQueryRequest>
{
    // Generous enough for "1 Year" plus slack, tight enough that a crafted ?from=
    // can't force an unbounded aggregation scan.
    private const int MaxSpanDays = 800;

    public OrderAnalyticsQueryRequestValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("'To' must be on or after 'From'.");

        RuleFor(x => x)
            .Must(x => (x.To - x.From).TotalDays <= MaxSpanDays)
            .WithMessage($"The date range can't span more than {MaxSpanDays} days.");
    }
}
