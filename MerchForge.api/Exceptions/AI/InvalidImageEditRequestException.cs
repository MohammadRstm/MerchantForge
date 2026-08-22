using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.AI
{
    /// <summary>No images, too many images, or a blank instruction — caught before any provider call is made.</summary>
    public class InvalidImageEditRequestException : AppException
    {
        public InvalidImageEditRequestException(string message) : base(
            Enums.ErrorType.Validation,
            "INVALID_IMAGE_EDIT_REQUEST",
            message)
        {
        }
    }
}
