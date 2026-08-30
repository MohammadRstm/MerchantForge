using FluentValidation;
using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.Validators.BusinessDashboard;

public class CreateOrderNoteRequestValidator : AbstractValidator<CreateOrderNoteRequest>
{
    public CreateOrderNoteRequestValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
