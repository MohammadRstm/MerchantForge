namespace MerchForge.api.DTOs.BusinessDashboard;

public class StockMovementResponse
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string ProductTitle { get; set; } = string.Empty;

    public int Amount { get; set; }

    public int BalanceAfter { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }
}
