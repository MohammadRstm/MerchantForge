using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard;

/// <summary>
/// Raised by DeleteProductAsync when the product has at least one OrderItem —
/// OrderItem.ProductId is Restrict-deleted precisely so a product with order history
/// can't be silently removed. There's no archive/deactivate flag yet, so today the
/// only fix is not deleting it; that's a real limitation, not just an error message.
/// </summary>
public class ProductHasOrdersException : AppException
{
    public ProductHasOrdersException() : base(
        Enums.ErrorType.Conflict,
        "PRODUCT_HAS_ORDERS",
        "This product can't be deleted because it appears in at least one order")
    {
    }
}
