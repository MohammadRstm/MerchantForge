using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Orders
{
    /// <summary>
    /// Raised when an order does not exist, or exists but belongs to a different
    /// business than the request is scoped to — both cases return the same error, same
    /// reasoning as ProductNotFoundException.
    /// </summary>
    public class OrderNotFoundException : AppException
    {
        public OrderNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "ORDER_NOT_FOUND",
            "Order was not found")
        {
        }
    }
}
