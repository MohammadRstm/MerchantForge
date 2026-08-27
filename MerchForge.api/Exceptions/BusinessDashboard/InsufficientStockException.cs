using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard;

/// <summary>
/// Raised when a Remove-Stock adjustment would take StockQuantity below zero —
/// including removing from an untracked (null) product, treated as 0 available.
/// </summary>
public class InsufficientStockException : AppException
{
    public InsufficientStockException() : base(
        Enums.ErrorType.Conflict,
        "INSUFFICIENT_STOCK",
        "That would take stock below zero.")
    {
    }
}
