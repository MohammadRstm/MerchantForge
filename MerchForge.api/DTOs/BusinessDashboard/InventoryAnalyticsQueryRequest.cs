namespace MerchForge.api.DTOs.BusinessDashboard;

public class InventoryAnalyticsQueryRequest
{
    /// <summary>Inclusive lower bound, UTC.</summary>
    public DateTime From { get; set; }

    /// <summary>Inclusive upper bound, UTC.</summary>
    public DateTime To { get; set; }
}
