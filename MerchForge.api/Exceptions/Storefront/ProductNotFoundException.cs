using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Storefront
{
    /// <summary>
    /// Raised when a product does not exist, or exists but belongs to a different
    /// business than the one the storefront request is scoped to. Both cases return
    /// the same error on purpose: a storefront for business A must not be able to
    /// probe whether a given product id exists under business B.
    /// </summary>
    public class ProductNotFoundException : AppException
    {
        public ProductNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "PRODUCT_NOT_FOUND",
            "Product was not found")
        {
        }
    }
}
