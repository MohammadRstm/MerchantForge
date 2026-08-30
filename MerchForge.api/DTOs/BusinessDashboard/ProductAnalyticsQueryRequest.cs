namespace MerchForge.api.DTOs.BusinessDashboard;

public class ProductAnalyticsQueryRequest
{
    /// <summary>Inclusive lower bound, UTC.</summary>
    public DateTime From { get; set; }

    /// <summary>Inclusive upper bound, UTC.</summary>
    public DateTime To { get; set; }

    /// <summary>
    /// When set, scopes the series to one product and adds its AllTime totals —
    /// powers the product-detail modal's trend chart. Null scopes to the whole
    /// catalog, for the Products page's main chart. Ignored by the performance-list
    /// endpoint, which is always catalog-wide.
    /// </summary>
    public Guid? ProductId { get; set; }
}
