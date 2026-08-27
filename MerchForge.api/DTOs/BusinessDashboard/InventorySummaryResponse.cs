namespace MerchForge.api.DTOs.BusinessDashboard;

public class InventorySummaryResponse
{
    public int TrackedProductCount { get; set; }

    public int UntrackedProductCount { get; set; }

    public int TotalUnitsInStock { get; set; }

    public int OutOfStockCount { get; set; }

    public int LowStockCount { get; set; }

    public int LowStockThreshold { get; set; }
}
