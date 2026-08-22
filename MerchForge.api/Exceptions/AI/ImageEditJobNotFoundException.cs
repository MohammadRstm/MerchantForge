using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.AI
{
    /// <summary>
    /// The job does not exist, or belongs to a different business. Both return the
    /// same error so one business cannot probe whether another's job id exists.
    /// </summary>
    public class ImageEditJobNotFoundException : AppException
    {
        public ImageEditJobNotFoundException() : base(
            Enums.ErrorType.NotFound,
            "IMAGE_EDIT_JOB_NOT_FOUND",
            "Image edit job was not found")
        {
        }
    }
}
