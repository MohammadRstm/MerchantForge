using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Orders
{
    /// <summary>Thrown when a status update doesn't match OrderStatus's allowed transitions (e.g. cancelling an already-Delivered order).</summary>
    public class OrderInvalidStatusTransitionException : AppException
    {
        public OrderInvalidStatusTransitionException() : base(
            Enums.ErrorType.Conflict,
            "ORDER_INVALID_STATUS_TRANSITION",
            "This order can't move to that status from its current status")
        {
        }
    }
}
