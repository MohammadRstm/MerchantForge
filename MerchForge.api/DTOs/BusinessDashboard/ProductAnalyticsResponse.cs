using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.BusinessDashboard;

public class ProductAnalyticsPointResponse
{
    /// <summary>Bucket start, UTC — a calendar day when Granularity is Daily, the first of the month when Monthly.</summary>
    public DateTime Period { get; set; }

    /// <summary>Excludes Cancelled orders.</summary>
    public decimal Revenue { get; set; }

    public int UnitsSold { get; set; }

    /// <summary>Distinct orders containing at least one qualifying item in this bucket.</summary>
    public int OrderCount { get; set; }
}

public class ProductAnalyticsPeriodTotalsResponse
{
    public decimal Revenue { get; set; }

    public int UnitsSold { get; set; }

    public int OrderCount { get; set; }
}

/// <summary>All-time totals for a single product — only populated when the request was scoped to one ProductId.</summary>
public class ProductAllTimeTotalsResponse
{
    public decimal Revenue { get; set; }

    public int UnitsSold { get; set; }

    public int OrderCount { get; set; }

    /// <summary>Null when OrderCount is 0.</summary>
    public decimal? AverageUnitsPerOrder { get; set; }
}

public class ProductAnalyticsResponse
{
    public OrderAnalyticsGranularity Granularity { get; set; }

    public List<ProductAnalyticsPointResponse> Points { get; set; } = [];

    public ProductAnalyticsPeriodTotalsResponse CurrentPeriod { get; set; } = new();

    /// <summary>The equal-length window immediately preceding CurrentPeriod — always populated, even when both totals are zero.</summary>
    public ProductAnalyticsPeriodTotalsResponse PreviousPeriod { get; set; } = new();

    /// <summary>Null when PreviousPeriod.Revenue is 0 — a percentage against zero isn't a real comparison.</summary>
    public decimal? RevenueChangePercent { get; set; }

    /// <summary>Null when PreviousPeriod.UnitsSold is 0.</summary>
    public decimal? UnitsSoldChangePercent { get; set; }

    /// <summary>Null when PreviousPeriod.OrderCount is 0.</summary>
    public decimal? OrderCountChangePercent { get; set; }

    public ProductAllTimeTotalsResponse? AllTime { get; set; }
}
