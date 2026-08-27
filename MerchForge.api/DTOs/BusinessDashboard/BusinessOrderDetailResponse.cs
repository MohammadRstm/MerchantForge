using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.BusinessDashboard;

public class BusinessOrderItemResponse
{
    public Guid ProductId { get; set; }

    public string ProductTitle { get; set; } = string.Empty;

    public string? ProductImageUrl { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }
}

public class BusinessOrderDetailResponse
{
    public Guid Id { get; set; }

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

    public OrderStatus Status { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    public string Currency { get; set; } = string.Empty;

    public List<BusinessOrderItemResponse> Items { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
