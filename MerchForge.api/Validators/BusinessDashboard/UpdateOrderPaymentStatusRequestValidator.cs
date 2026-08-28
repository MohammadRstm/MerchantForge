using FluentValidation;
using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.Validators.BusinessDashboard;

public class UpdateOrderPaymentStatusRequestValidator : AbstractValidator<UpdateOrderPaymentStatusRequest>
{
    public UpdateOrderPaymentStatusRequestValidator()
    {
        RuleFor(x => x.PaymentStatus)
            .IsInEnum();
    }
}
