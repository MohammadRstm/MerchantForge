namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// Global order counts for this business, unaffected by whatever search/status/date
/// filters the order list currently has applied — powers the Orders page's KPI cards
/// and its "needs attention" section.
/// </summary>
public class OrderStatsResponse
{
    public int TotalCount { get; set; }

    public int PendingCount { get; set; }

    public int ConfirmedCount { get; set; }

    public int ShippedCount { get; set; }

    public int DeliveredCount { get; set; }

    public int CancelledCount { get; set; }

    /// <summary>Pending orders older than the "needs attention" staleness threshold (24h).</summary>
    public int StalePendingCount { get; set; }

    /// <summary>When the oldest still-Pending order was placed — null when there are none.</summary>
    public DateTime? OldestPendingOrderCreatedAt { get; set; }

    /// <summary>Orders cancelled within the last 24h.</summary>
    public int RecentlyCancelledCount { get; set; }
}
