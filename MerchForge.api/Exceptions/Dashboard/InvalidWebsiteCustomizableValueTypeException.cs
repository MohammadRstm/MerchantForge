using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class InvalidWebsiteCustomizableValueTypeException : AppException
    {
        public InvalidWebsiteCustomizableValueTypeException() : base(
            Enums.ErrorType.Validation,
            "INVALID_WEBSITE_CUSTOMIZABLE_VALUE_TYPE",
            "Value type must be one of Text, Textarea, Image, Color, Url, Boolean, Number, Select, Link")
        {
        }
    }
}
