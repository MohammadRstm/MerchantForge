using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class ProductAttributeDefinitionNotFoundException : AppException
    {
        public ProductAttributeDefinitionNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "PRODUCT_ATTRIBUTE_DEFINITION_NOT_FOUND",
            "Product field was not found")
        {
        }
    }
}
