using FluentValidation;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Validators.Common;

namespace MerchForge.api.Validators.Storefront;

public class StorefrontProductsQueryRequestValidator
    : PagedQueryValidator<StorefrontProductsQueryRequest>
{
    public StorefrontProductsQueryRequestValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(255);

        RuleFor(x => x.SortBy)
            .IsInEnum();

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxPrice.HasValue);

        // An inverted range silently returns nothing, which looks like "no products"
        // rather than "bad request". Reject it so storefronts find the bug.
        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice!.Value)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage("'Max Price' must be greater than or equal to 'Min Price'.");
    }
}
