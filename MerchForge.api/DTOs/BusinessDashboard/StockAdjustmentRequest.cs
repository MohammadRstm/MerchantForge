namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// Signed so it flows straight into StockMovement.Amount — the "Add Stock" and
/// "Remove Stock" buttons both hit the same endpoint, just with a positive or
/// negative Amount.
/// </summary>
public class StockAdjustmentRequest
{
    public int Amount { get; set; }

    public string? Reason { get; set; }
}
