using MerchForge.api.Enums;

namespace MerchForge.api.Models;

/// <summary>
/// A customer's order, placed anonymously through the storefront — there is no
/// customer-account system yet, so every customer/shipping field here is a snapshot
/// entered at checkout, not a reference to anything else. Money fields are real
/// (Subtotal/Total/Currency) but PaymentStatus starts and generally stays Pending
/// until a payment gateway exists; see PaymentStatus's own doc comment.
/// </summary>
public class Order
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public string? CustomerPhone { get; set; }

    public string ShippingAddressLine1 { get; set; } = string.Empty;

    public string? ShippingAddressLine2 { get; set; }

    public string ShippingCity { get; set; } = string.Empty;

    public string? ShippingState { get; set; }

    public string ShippingPostalCode { get; set; } = string.Empty;

    public string ShippingCountry { get; set; } = string.Empty;

    public string? CustomerNotes { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    /// <summary>Sum of every OrderItem.LineTotal at creation time.</summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Equal to Subtotal today — no shipping cost or tax calculation exists yet. Kept
    /// as its own column (rather than derived at read time) so those can be added
    /// later without a schema change or breaking any already-placed order's total.
    /// </summary>
    public decimal Total { get; set; }

    /// <summary>Snapshot of Business.Currency at order time — a later currency change on the business must not rewrite historical orders.</summary>
    public string Currency { get; set; } = "USD";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Business Business { get; set; } = null!;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
