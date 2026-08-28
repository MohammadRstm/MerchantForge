using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>One row in the owner's order list — no items, no shipping address; see BusinessOrderDetailResponse for the full order.</summary>
public class BusinessOrderResponse
{
    public Guid Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public decimal Total { get; set; }

    public string Currency { get; set; } = string.Empty;

    public int ItemCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
