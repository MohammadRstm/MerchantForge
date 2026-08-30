using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.BusinessDashboard;

public class InventoryAnalyticsPointResponse
{
    /// <summary>Bucket start, UTC — a calendar day when Granularity is Daily, the first of the month when Monthly.</summary>
    public DateTime Period { get; set; }

    /// <summary>Excludes Cancelled orders — from OrderItem, same convention as product/order analytics.</summary>
    public int UnitsSold { get; set; }

    /// <summary>Sum of positive StockMovement.Amount in this bucket (manual restocks and order-cancellation reversals both count).</summary>
    public int StockAdded { get; set; }

    /// <summary>Sum of the absolute value of negative StockMovement.Amount in this bucket (manual removals and order-driven decrements both count).</summary>
    public int StockRemoved { get; set; }
}

public class InventoryAnalyticsPeriodTotalsResponse
{
    public int UnitsSold { get; set; }

    public int StockAdded { get; set; }

    public int StockRemoved { get; set; }
}

public class InventoryAnalyticsResponse
{
    public OrderAnalyticsGranularity Granularity { get; set; }

    public List<InventoryAnalyticsPointResponse> Points { get; set; } = [];

    public InventoryAnalyticsPeriodTotalsResponse CurrentPeriod { get; set; } = new();

    /// <summary>The equal-length window immediately preceding CurrentPeriod — always populated, even when every total is zero.</summary>
    public InventoryAnalyticsPeriodTotalsResponse PreviousPeriod { get; set; } = new();

    /// <summary>Null when PreviousPeriod.UnitsSold is 0.</summary>
    public decimal? UnitsSoldChangePercent { get; set; }
}
