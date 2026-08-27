namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// Returns both the updated product and the movement that was just recorded, so the
/// frontend can update the product row and prepend to the recent-activity list
/// without a refetch.
/// </summary>
public class StockAdjustmentResponse
{
    public BusinessProductResponse Product { get; set; } = null!;

    public StockMovementResponse Movement { get; set; } = null!;
}
