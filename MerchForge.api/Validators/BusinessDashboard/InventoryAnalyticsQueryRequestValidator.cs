using FluentValidation;
using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.Validators.BusinessDashboard;

public class InventoryAnalyticsQueryRequestValidator : AbstractValidator<InventoryAnalyticsQueryRequest>
{
    // Matches the Orders/Products analytics validators' cap.
    private const int MaxSpanDays = 800;

    public InventoryAnalyticsQueryRequestValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("'To' must be on or after 'From'.");

        RuleFor(x => x)
            .Must(x => (x.To - x.From).TotalDays <= MaxSpanDays)
            .WithMessage($"The date range can't span more than {MaxSpanDays} days.");
    }
}
