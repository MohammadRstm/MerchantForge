using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Storefront
{
    /// <summary>
    /// Deliberately the same outcome (and same message) whether the business doesn't
    /// exist, has no draft yet, or the token just doesn't match — this endpoint must
    /// never be usable to probe which of those is true.
    /// </summary>
    public class InvalidPreviewTokenException : AppException
    {
        public InvalidPreviewTokenException() : base(
            Enums.ErrorType.NotFound,
            "INVALID_PREVIEW_TOKEN",
            "No preview found for that business and token")
        {
        }
    }
}
