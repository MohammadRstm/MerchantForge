using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class InvalidMetadataFieldKeyException : AppException
    {
        public InvalidMetadataFieldKeyException() : base(
            Enums.ErrorType.Validation,
            "INVALID_METADATA_FIELD_KEY",
            "One or more fields are not part of this business's domain catalogue")
        {
        }
    }
}
