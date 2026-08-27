using FluentValidation;
using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.Validators.BusinessDashboard;

public class UpdateLowStockThresholdRequestValidator : AbstractValidator<UpdateLowStockThresholdRequest>
{
    public UpdateLowStockThresholdRequestValidator()
    {
        // Mirrors the database CHECK constraint (CK_businesses_LowStockThreshold_Positive).
        RuleFor(x => x.LowStockThreshold)
            .InclusiveBetween(1, 100_000);
    }
}
