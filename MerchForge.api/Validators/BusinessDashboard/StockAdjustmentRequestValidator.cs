using FluentValidation;
using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.Validators.BusinessDashboard;

public class StockAdjustmentRequestValidator : AbstractValidator<StockAdjustmentRequest>
{
    public StockAdjustmentRequestValidator()
    {
        // Mirrors the database CHECK constraint (CK_stock_movements_Amount_NotZero).
        RuleFor(x => x.Amount)
            .NotEqual(0)
            .WithMessage("Enter a non-zero amount.");

        RuleFor(x => x.Reason)
            .MaximumLength(255);
    }
}
