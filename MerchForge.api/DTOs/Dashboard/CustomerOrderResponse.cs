namespace MerchForge.api.DTOs.Dashboard;

/// <summary>
/// One row in a customer's order history. No human-readable order number exists in
/// the schema (Order only has a Guid Id) - the frontend derives a short display
/// reference from Id rather than a fabricated sequential number.
/// </summary>
public class CustomerOrderResponse
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
