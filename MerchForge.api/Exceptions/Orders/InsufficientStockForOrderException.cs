using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Orders
{
    /// <summary>
    /// Raised when placing an order would take a tracked product's stock below zero.
    /// The whole order is rejected — no partial orders — so a customer never gets
    /// billed for units that aren't actually there.
    /// </summary>
    public class InsufficientStockForOrderException : AppException
    {
        public InsufficientStockForOrderException(string productTitle) : base(
            Enums.ErrorType.Conflict,
            "INSUFFICIENT_STOCK_FOR_ORDER",
            $"Not enough stock for '{productTitle}' to fulfill this order")
        {
        }
    }
}
