using MerchForge.api.DTOs.Common;
using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.Storefront;

/// <summary>
/// Extends the existing PagedQuery rather than introducing a second pagination
/// convention. Every filter here is backed by an actual column, so nothing is
/// accepted that the schema cannot honour.
/// </summary>
public class StorefrontProductsQueryRequest : PagedQuery
{
    /// <summary>Matched against the product title.</summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filters by category id rather than name. Names are display values and are not
    /// unique across domains ("Accessories" exists under both Fashion and
    /// Electronics), so they are not a safe filter key.
    /// </summary>
    public Guid? CategoryId { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    /// <summary>Reuses the existing ProductSortField (CreatedAt | Title | Price).</summary>
    public ProductSortField SortBy { get; set; } = ProductSortField.CreatedAt;

    public bool SortDescending { get; set; } = true;
}
