using MerchForge.api.Enums;

namespace MerchForge.api.Models;

/// <summary>
/// One entry in an order's real status timeline. Written once when the order is
/// placed (Status = Pending, ChangedByUserId = Guid.Empty — the same "system/customer
/// driven" sentinel StockMovement.CreatedByUserId already uses for actions no
/// dashboard user initiated) and again every time UpdateOrderStatusAsync moves the
/// order to a new status, in the same transaction as the status change itself. Never
/// backfilled for orders that existed before this table did — those simply have no
/// history rows before their next status change.
/// </summary>
public class OrderStatusHistory
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public OrderStatus Status { get; set; }

    public Guid ChangedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
}
