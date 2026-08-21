using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.AI
{
    /// <summary>
    /// The image-editing provider failed, or returned something with no image in it.
    /// Surfaced as Unexpected rather than Validation: nothing the owner typed caused
    /// it.
    /// </summary>
    public class ImageEditingException : AppException
    {
        public ImageEditingException(string message, Exception? inner = null) : base(
            Enums.ErrorType.Unexpected,
            "IMAGE_EDITING_FAILED",
            message)
        {
            InnerFailure = inner;
        }

        /// <summary>Kept for logging; never serialized to the client.</summary>
        public Exception? InnerFailure { get; }
    }
}
