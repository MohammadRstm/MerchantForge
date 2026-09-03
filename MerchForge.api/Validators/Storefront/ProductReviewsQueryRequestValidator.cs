using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Validators.Common;

namespace MerchForge.api.Validators.Storefront;

/// <summary>
/// Page and page-size bounds only — ProductReviewsQueryRequest adds no fields of its
/// own, so the shared base is the whole rule set.
/// </summary>
public class ProductReviewsQueryRequestValidator
    : PagedQueryValidator<ProductReviewsQueryRequest>
{
}
