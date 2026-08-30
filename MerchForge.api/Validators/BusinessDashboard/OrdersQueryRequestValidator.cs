using FluentValidation;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.Validators.Common;

namespace MerchForge.api.Validators.BusinessDashboard;

public class OrdersQueryRequestValidator : PagedQueryValidator<OrdersQueryRequest>
{
    public OrdersQueryRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);

        RuleFor(x => x.Search)
            .MaximumLength(255);

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From.HasValue && x.To.HasValue);
    }
}
