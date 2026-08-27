using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class InvalidProductAttributeValueTypeException : AppException
    {
        public InvalidProductAttributeValueTypeException() : base(
            Enums.ErrorType.Validation,
            "INVALID_PRODUCT_ATTRIBUTE_VALUE_TYPE",
            "Value type must be one of Text, Number, Boolean, TextList, ColorList")
        {
        }
    }
}
