using FluentValidation;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Validators.Common;

namespace MerchForge.api.Validators.Dashboard;

public class UsersQueryRequestValidator : PagedQueryValidator<UsersQueryRequest>
{
    public UsersQueryRequestValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(255);

        RuleFor(x => x.SortBy)
            .IsInEnum();
    }
}
