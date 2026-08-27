using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class ProductAttributeKeyAlreadyExistsException : AppException
    {
        public ProductAttributeKeyAlreadyExistsException() : base(
            Enums.ErrorType.Conflict,
            "PRODUCT_ATTRIBUTE_KEY_ALREADY_EXISTS",
            "A field with this key already exists for this domain")
        {
        }
    }
}
