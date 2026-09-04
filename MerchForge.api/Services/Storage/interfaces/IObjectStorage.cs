namespace MerchForge.api.Services.Storage.interfaces
{
    /// <summary>
    /// A flat, keyed byte store. Deliberately knows nothing about businesses,
    /// products or URLs — what a key means is the caller's concern, so the same
    /// abstraction serves product images today and anything else later.
    ///
    /// Implementations translate provider failures into ObjectStorageException so no
    /// caller ends up depending on a specific SDK's exception types.
    /// </summary>
    public interface IObjectStorage
    {
        /// <summary>
        /// Writes (or overwrites) the object at <paramref name="key"/>.
        ///
        /// contentType is stored on the object and is what the CDN will serve it back
        /// as, so callers must pass a type they have actually verified rather than one
        /// a client claimed.
        /// </summary>
        Task PutAsync(
            string key,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the object back. Throws ObjectStorageException when it doesn't exist.
        /// </summary>
        Task<(byte[] Bytes, string ContentType)> GetAsync(
            string key,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes one object. Deleting a key that isn't there is not an error — the
        /// desired end state is the same either way.
        /// </summary>
        Task DeleteAsync(
            string key,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes many objects in as few round trips as the provider allows. Same
        /// tolerance for absent keys as DeleteAsync.
        /// </summary>
        Task DeleteManyAsync(
            IReadOnlyCollection<string> keys,
            CancellationToken cancellationToken = default);
    }
}
