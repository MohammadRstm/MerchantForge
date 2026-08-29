using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Storefront;
using MerchForge.api.Enums;
using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces;

/// <summary>
/// Data access for orders, shared by both the anonymous storefront (creation, single
/// lookup) and the authenticated owner dashboard (listing, detail, status changes) —
/// there is exactly one source of truth for how an order is written, so stock never
/// gets decremented in two different ways.
/// </summary>
public interface IOrderRepository
{
    // ---- storefront ----

    /// <summary>
    /// Validates every requested product belongs to this business and has enough
    /// stock, decrements tracked products' stock with a matching StockMovement row,
    /// and persists the order — all in one transaction. Throws ProductNotFoundException
    /// for an unknown/foreign product id, InsufficientStockForOrderException for the
    /// first tracked item that doesn't have enough left.
    /// </summary>
    Task<Order> CreateOrderAsync(
        Guid businessId,
        CreateOrderRequest request,
        Guid? customerId,
        CancellationToken cancellationToken = default);

    /// <summary>Null when the order doesn't exist or belongs to a different business.</summary>
    Task<StorefrontOrderResponse?> GetOrderForStorefrontAsync(
        Guid businessId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    // ---- dashboard ----

    Task<(List<BusinessOrderResponse> Items, int TotalCount)> GetOrdersAsync(
        Guid businessId,
        OrdersQueryRequest query,
        CancellationToken cancellationToken = default);

    Task<BusinessOrderDetailResponse?> GetOrderAsync(
        Guid businessId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    /// <summary>Loads the tracked order (with items) scoped to the business, for a status/payment-status update.</summary>
    Task<Order?> GetTrackedOrderAsync(
        Guid businessId,
        Guid orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the new status and appends a matching OrderStatusHistory row. When
    /// moving into Cancelled, restocks every tracked item on the order (reversing
    /// CreateOrderAsync's decrement) with a matching positive StockMovement row — all
    /// in the same transaction as the status change.
    /// </summary>
    Task UpdateOrderStatusAsync(
        Order order,
        OrderStatus newStatus,
        Guid changedByUserId,
        CancellationToken cancellationToken = default);

    Task UpdateOrderPaymentStatusAsync(
        Order order,
        PaymentStatus newStatus,
        CancellationToken cancellationToken = default);

    Task<int> CountOrdersAsync(Guid businessId, CancellationToken cancellationToken = default);

    Task<int> CountOrdersByStatusAsync(Guid businessId, OrderStatus status, CancellationToken cancellationToken = default);

    /// <summary>Whether any OrderItem references this product — guards product deletion (OrderItem.ProductId is Restrict-deleted).</summary>
    Task<bool> HasOrderItemsForProductAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>Global per-status counts plus the "needs attention" signals, unaffected by any list filter.</summary>
    Task<OrderStatsResponse> GetOrderStatsAsync(Guid businessId, CancellationToken cancellationToken = default);

    /// <summary>Newest first. Throws OrderNotFoundException if the order doesn't exist or isn't this business's.</summary>
    Task<List<OrderNoteResponse>> GetOrderNotesAsync(Guid businessId, Guid orderId, CancellationToken cancellationToken = default);

    /// <summary>Throws OrderNotFoundException if the order doesn't exist or isn't this business's.</summary>
    Task<OrderNoteResponse> AddOrderNoteAsync(
        Guid businessId,
        Guid orderId,
        string content,
        Guid createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Oldest first — a lifecycle timeline. Throws OrderNotFoundException if the order doesn't exist or isn't this business's.</summary>
    Task<List<OrderStatusHistoryEntryResponse>> GetOrderStatusHistoryAsync(Guid businessId, Guid orderId, CancellationToken cancellationToken = default);
}
