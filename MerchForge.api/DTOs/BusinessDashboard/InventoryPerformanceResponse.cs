namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// One product's inventory-relevant performance for the requested period — every
/// product in the catalog gets a row, tracked or not, so the frontend can derive
/// fast-movers, slow/dead stock, and restock-risk views from one bounded
/// (catalog-sized) list without a per-view round trip. Velocity, days-of-stock-
/// remaining, and risk category are deliberately NOT computed here — they're pure
/// derived math from UnitsSold/StockQuantity/the period length the frontend already
/// knows, so keeping them client-side avoids duplicating "how long is this period"
/// logic on the backend.
/// </summary>
public class InventoryProductPerformanceEntryResponse
{
    public Guid ProductId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Null means this product isn't tracked.</summary>
    public int? StockQuantity { get; set; }

    /// <summary>Units sold in the requested period, excludes Cancelled orders.</summary>
    public int UnitsSold { get; set; }

    public decimal Revenue { get; set; }

    /// <summary>All-time, not period-scoped — null if this product has never sold. Powers "dead stock" / last-sale framing regardless of which period is selected.</summary>
    public DateTime? LastSaleAt { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class InventoryCategoryPerformanceEntryResponse
{
    public string CategoryName { get; set; } = string.Empty;

    public int TrackedProductCount { get; set; }

    public int UntrackedProductCount { get; set; }

    public int UnitsInStock { get; set; }

    public int UnitsSold { get; set; }

    public decimal Revenue { get; set; }

    public int LowStockCount { get; set; }

    public int OutOfStockCount { get; set; }
}

public class InventoryPerformanceResponse
{
    public List<InventoryProductPerformanceEntryResponse> Products { get; set; } = [];

    public List<InventoryCategoryPerformanceEntryResponse> Categories { get; set; } = [];
}
