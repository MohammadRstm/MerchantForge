namespace MerchForge.api.Models;

/// <summary>
/// One line of an order. Title/ImageUrl/UnitPrice are snapshotted at order time —
/// an order's history must read the same years later even if the product it
/// referenced is since edited, re-priced, or deleted (ProductId is Restrict-deleted
/// for exactly this reason: see OrderItemConfiguration).
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public Guid ProductId { get; set; }

    public string ProductTitle { get; set; } = string.Empty;

    public string? ProductImageUrl { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    /// <summary>UnitPrice * Quantity, stored rather than computed — same convention as Order.Total.</summary>
    public decimal LineTotal { get; set; }

    public Order Order { get; set; } = null!;
}
