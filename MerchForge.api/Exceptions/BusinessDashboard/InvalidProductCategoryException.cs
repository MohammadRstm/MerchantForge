using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.BusinessDashboard
{
    /// <summary>
    /// Raised when a product references a category the business may not use — one
    /// from a different domain, or another business's private custom category.
    ///
    /// This is the write-time enforcement of the invariant the schema cannot express:
    /// product.Category.BusinessDomainId must equal product.Business.BusinessDomainId,
    /// which MariaDB cannot state as a CHECK because it needs a subquery.
    /// </summary>
    public class InvalidProductCategoryException : AppException
    {
        public InvalidProductCategoryException() : base(
            Enums.ErrorType.Validation,
            "INVALID_PRODUCT_CATEGORY",
            "That category isn't available for this business.")
        {
        }
    }
}
