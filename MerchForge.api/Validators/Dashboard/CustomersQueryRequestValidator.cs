using FluentValidation;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Validators.Common;

namespace MerchForge.api.Validators.Dashboard;

public class CustomersQueryRequestValidator : PagedQueryValidator<CustomersQueryRequest>
{
    public CustomersQueryRequestValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(255);

        RuleFor(x => x.SortBy)
            .IsInEnum();

        RuleFor(x => x)
            .Must(x => !x.RegisteredFrom.HasValue || !x.RegisteredTo.HasValue || x.RegisteredFrom <= x.RegisteredTo)
            .WithMessage("RegisteredFrom must be before or equal to RegisteredTo.");
    }
}
