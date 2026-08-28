namespace MerchForge.api.DTOs.Storefront;

public class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }

    public int Quantity { get; set; }
}

/// <summary>
/// Submitted from the storefront's checkout form. Deliberately carries no price —
/// StorefrontService looks up each product's real, current Price itself; a client
/// could otherwise submit whatever total it wanted.
/// </summary>
public class CreateOrderRequest
{
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

    public List<CreateOrderItemRequest> Items { get; set; } = [];
}
