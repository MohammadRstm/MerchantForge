using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.AI
{
    /// <summary>
    /// The draft does not exist, or belongs to a different business. Both return the
    /// same error so one business cannot probe whether another's draft id exists.
    /// </summary>
    public class ProductDraftNotFoundException : AppException
    {
        public ProductDraftNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "PRODUCT_DRAFT_NOT_FOUND",
            "Product draft was not found")
        {
        }
    }
}
