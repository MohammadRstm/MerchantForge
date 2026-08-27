using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Dashboard
{
    public class MetadataShapeDomainRequiredException : AppException
    {
        public MetadataShapeDomainRequiredException() : base(
            Enums.ErrorType.Validation,
            "METADATA_SHAPE_DOMAIN_REQUIRED",
            "This business has no domain set, so it has no metadata field catalogue to choose from yet")
        {
        }
    }
}
