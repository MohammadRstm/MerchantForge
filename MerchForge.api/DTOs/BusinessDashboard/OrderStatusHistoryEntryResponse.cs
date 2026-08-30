using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.BusinessDashboard;

public class OrderStatusHistoryEntryResponse
{
    public OrderStatus Status { get; set; }

    /// <summary>Null for the initial "order placed" entry, which the customer/storefront triggers, not a dashboard user.</summary>
    public string? ChangedByUserName { get; set; }

    public DateTime CreatedAt { get; set; }
}
