namespace MerchForge.api.Models;

/// <summary>
/// An internal note a dashboard user leaves on an order (e.g. "call before delivery").
/// Always dashboard-authored — CreatedByUserId is always a real User, never the
/// Guid.Empty "customer/system" sentinel OrderStatusHistory uses. Never shown to the
/// customer; there is no storefront-facing surface that reads this table.
/// </summary>
public class OrderNote
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string Content { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
}
