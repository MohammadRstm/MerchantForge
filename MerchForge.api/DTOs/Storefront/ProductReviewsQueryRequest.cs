using MerchForge.api.DTOs.Common;

namespace MerchForge.api.DTOs.Storefront;

/// <summary>
/// Paging for a product's review list. Deliberately has no sort or filter options:
/// reviews are always read newest-first, and the only filter that exists — visible
/// versus hidden — is decided by which surface is asking, never by the caller.
/// </summary>
public class ProductReviewsQueryRequest : PagedQuery
{
}
