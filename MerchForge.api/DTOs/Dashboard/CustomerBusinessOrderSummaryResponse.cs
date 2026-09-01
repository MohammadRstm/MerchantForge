namespace MerchForge.api.DTOs.Dashboard;

/// <summary>
/// One row per business this customer has ordered from — derived live from
/// Orders.Where(o => o.CustomerId == id) grouped by business, never a stored
/// relation, since a Customer is otherwise completely independent of any business.
/// </summary>
public class CustomerBusinessOrderSummaryResponse
{
    public Guid BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public int OrderCount { get; set; }

    public decimal TotalSpent { get; set; }

    public string Currency { get; set; } = "USD";

    public DateTime? LastOrderAt { get; set; }
}
