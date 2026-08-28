namespace MerchForge.api.Enums;

/// <summary>
/// No payment gateway is wired up yet — every order is created with PaymentStatus
/// Pending, and today only a dashboard owner can flip it (e.g. cash/bank transfer
/// reconciled by hand). This is deliberately a placeholder: when a real gateway is
/// added, its webhook becomes the thing that sets Paid/Refunded instead of the owner
/// doing it manually — nothing about the Order/OrderItem shape needs to change.
/// </summary>
public enum PaymentStatus
{
    Pending,
    Paid,
    Refunded,
}
