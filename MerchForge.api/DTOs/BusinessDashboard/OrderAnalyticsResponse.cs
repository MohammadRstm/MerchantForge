using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.BusinessDashboard;

public class OrderAnalyticsPointResponse
{
    /// <summary>Bucket start, UTC — a calendar day when Granularity is Daily, the first of the month when Monthly.</summary>
    public DateTime Period { get; set; }

    /// <summary>Excludes Cancelled orders — see OrderRepository.GetOrderAnalyticsAsync's own doc comment.</summary>
    public int OrderCount { get; set; }

    public decimal Revenue { get; set; }
}

public class OrderAnalyticsPeriodTotalsResponse
{
    public int OrderCount { get; set; }

    public decimal Revenue { get; set; }
}

public class OrderAnalyticsResponse
{
    public OrderAnalyticsGranularity Granularity { get; set; }

    public List<OrderAnalyticsPointResponse> Points { get; set; } = [];

    public OrderAnalyticsPeriodTotalsResponse CurrentPeriod { get; set; } = new();

    /// <summary>The equal-length window immediately preceding CurrentPeriod — always populated, even when both totals are zero.</summary>
    public OrderAnalyticsPeriodTotalsResponse PreviousPeriod { get; set; } = new();

    /// <summary>Null when PreviousPeriod.Revenue is 0 — a percentage against zero isn't a real comparison.</summary>
    public decimal? RevenueChangePercent { get; set; }

    /// <summary>Null when PreviousPeriod.OrderCount is 0.</summary>
    public decimal? OrderCountChangePercent { get; set; }
}
