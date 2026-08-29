namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// One product's performance for the requested period, plus enough of the previous
/// equal-length period to compute a trend — every product in the catalog gets a row
/// here, including ones with zero sales, so the frontend can derive top-N rankings,
/// revenue distribution, best sellers, "needs attention", and zero-sales sections all
/// from this one bounded (catalog-sized, not order-volume-sized) list.
/// </summary>
public class ProductPerformanceEntryResponse
{
    public Guid ProductId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    /// <summary>Current period, excludes Cancelled orders.</summary>
    public int UnitsSold { get; set; }

    public decimal Revenue { get; set; }

    public int OrderCount { get; set; }

    /// <summary>Previous equal-length period — for trend indicators only, not displayed as its own figure.</summary>
    public int PreviousUnitsSold { get; set; }

    public decimal PreviousRevenue { get; set; }

    /// <summary>Null when PreviousUnitsSold is 0.</summary>
    public decimal? UnitsSoldChangePercent { get; set; }

    /// <summary>Null when PreviousRevenue is 0.</summary>
    public decimal? RevenueChangePercent { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class CategoryPerformanceEntryResponse
{
    public string CategoryName { get; set; } = string.Empty;

    public int ProductCount { get; set; }

    public int UnitsSold { get; set; }

    public decimal Revenue { get; set; }
}

public class ProductPerformanceResponse
{
    public List<ProductPerformanceEntryResponse> Products { get; set; } = [];

    public List<CategoryPerformanceEntryResponse> Categories { get; set; } = [];

    /// <summary>Sum of every product's Revenue for the period — the denominator for revenue-distribution percentages.</summary>
    public decimal TotalRevenue { get; set; }
}
