namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// Catalog-wide, all-time — deliberately not scoped to the analytics date range (see
/// ProductAnalyticsResponse for the time-ranged numbers). No "active/published"
/// count: Product has no such field today.
/// </summary>
public class ProductCatalogOverviewResponse
{
    public int TotalProducts { get; set; }

    /// <summary>All-time, excludes Cancelled orders.</summary>
    public int TotalUnitsSold { get; set; }

    /// <summary>All-time, excludes Cancelled orders.</summary>
    public decimal ProductRevenue { get; set; }

    /// <summary>Null when the catalog is empty.</summary>
    public decimal? AverageProductPrice { get; set; }
}
