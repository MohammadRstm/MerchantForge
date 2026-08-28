namespace MerchForge.api.Enums;

/// <summary>
/// Fulfillment lifecycle, entirely separate from PaymentStatus — an order can be
/// Confirmed and shipped while payment is still Pending (e.g. cash/bank transfer on
/// delivery), which is a real and common case, not a data-integrity problem.
///
/// Allowed transitions: Pending -> Confirmed | Cancelled; Confirmed -> Shipped |
/// Cancelled; Shipped -> Delivered. Delivered and Cancelled are terminal.
/// </summary>
public enum OrderStatus
{
    Pending,
    Confirmed,
    Shipped,
    Delivered,
    Cancelled,
}
