namespace MerchForge.api.Exceptions.Storage
{
    /// <summary>
    /// The object store answered, and the key is not there.
    ///
    /// Separate from the general ObjectStorageException so callers can tell a missing
    /// object from an unreachable bucket. Collapsing the two would report an outage as
    /// a client error, and report a genuinely absent image as a 500.
    /// </summary>
    public class ObjectNotFoundException : ObjectStorageException
    {
        public ObjectNotFoundException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
