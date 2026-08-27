using FluentValidation;
using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.Validators.Common;

namespace MerchForge.api.Validators.BusinessDashboard;

public class ProductsQueryRequestValidator : PagedQueryValidator<ProductsQueryRequest>
{
    public ProductsQueryRequestValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(255);

        RuleFor(x => x.Category)
            .MaximumLength(100);

        RuleFor(x => x.SortBy)
            .IsInEnum();

        RuleFor(x => x.StockStatus)
            .IsInEnum();
    }
}
