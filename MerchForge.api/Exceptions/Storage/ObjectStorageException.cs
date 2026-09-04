using MerchForge.api.Exceptions.Base;

namespace MerchForge.api.Exceptions.Storage
{
    /// <summary>
    /// An object store operation failed for a reason that isn't the caller's fault —
    /// the bucket was unreachable, credentials were rejected, the object was missing.
    ///
    /// Exists so nothing above IObjectStorage has to reference AmazonS3Exception: the
    /// storage implementation is meant to be swappable, and a caller catching an AWS
    /// SDK type would quietly weld it in place. The message is deliberately generic —
    /// the underlying exception carries endpoint and request detail that belongs in
    /// the log, not in a response body.
    /// </summary>
    public class ObjectStorageException : AppException
    {
        public ObjectStorageException(string message, Exception? innerException = null) : base(
            Enums.ErrorType.Unexpected,
            "OBJECT_STORAGE_FAILURE",
            message)
        {
            InnerStorageException = innerException;
        }

        /// <summary>
        /// The provider exception, kept for logging. AppException's own constructor
        /// doesn't take an inner exception, so it's carried here instead.
        /// </summary>
        public Exception? InnerStorageException { get; }
    }
}
