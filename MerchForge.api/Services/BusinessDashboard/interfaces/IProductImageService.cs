namespace MerchForge.api.Services.BusinessDashboard.interfaces
{
    /// <summary>
    /// Where an image ended up, and how big it actually is once stored.
    ///
    /// Dimensions come back from here because the stored image is not always the file
    /// that was uploaded - an oversized photo is scaled down first - so measuring it
    /// in the browser would describe the wrong image.
    /// </summary>
    public record StoredImage(string Key, int? Width, int? Height);

    /// <summary>
    /// Product image storage.
    ///
    /// Every method returns and accepts the value that goes in the database, which is
    /// an object key rather than a URL. Turning that into something a browser can load
    /// happens at the API boundary, via IProductImageUrlResolver - see that type for
    /// why the delivery origin is kept out of persisted data.
    /// </summary>
    public interface IProductImageService
    {
        /// <summary>
        /// Validates, shrinks and stores an uploaded product image, returning the object
        /// key to save on the product. Throws InvalidProductImageException when the file is
        /// empty, too large, or not genuinely an image of an allowed type.
        ///
        /// productId places the image inside its product within the business, and does
        /// not have to exist yet - the form uploads images before the product is
        /// committed, so a new product's id is chosen by the client up front. What is
        /// checked is that the id is not already owned by a different business.
        /// businessId always comes from the authorized route, never from the client, so
        /// a cross-tenant write is impossible whatever productId is passed.
        /// </summary>
        Task<StoredImage> SaveAsync(
            Guid businessId,
            Guid productId,
            IFormFile file,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Same validation and storage as the upload path, for bytes that didn't come
        /// through a form upload - an AI-edited image, say. contentType is trusted
        /// here rather than re-derived, since the caller already knows what it asked
        /// the provider to return; the byte-signature check still applies.
        /// </summary>
        Task<StoredImage> SaveAsync(
            Guid businessId,
            Guid productId,
            byte[] bytes,
            string contentType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads back the bytes of a previously-stored image, verifying first that it
        /// actually belongs to this business - a caller passing another business's
        /// image, or an arbitrary path, gets the same rejection as one that doesn't
        /// exist. Throws InvalidProductImageException on either.
        ///
        /// Accepts both an object key and the API-relative path of an image stored
        /// before the move to object storage, so editing a pre-migration product keeps
        /// working.
        /// </summary>
        Task<(byte[] Bytes, string ContentType)> ReadAsync(
            Guid businessId,
            string storedValue,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Best-effort cleanup of images that no longer have a row pointing at them.
        ///
        /// Never throws: the database is the source of truth, so a storage failure
        /// here leaves an orphaned object that costs a little space, whereas failing
        /// the caller would undo a delete the user has already been told succeeded.
        /// Values belonging to another business, and images still on local disk, are
        /// skipped rather than rejected.
        /// </summary>
        Task DeleteManyAsync(
            Guid businessId,
            IReadOnlyCollection<string> storedValues,
            CancellationToken cancellationToken = default);
    }
}
