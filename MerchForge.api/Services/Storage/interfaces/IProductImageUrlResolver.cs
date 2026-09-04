namespace MerchForge.api.Services.Storage.interfaces
{
    /// <summary>
    /// The single owner of the product-image object-key format, and of the boundary
    /// between the key stored in the database and the URL a browser loads.
    ///
    /// Building and parsing live together deliberately: the key layout is expressed
    /// once, so the two halves cannot drift into disagreeing about what a valid key
    /// looks like.
    ///
    /// The database stores keys, never URLs. Keeping the delivery origin out of
    /// persisted data is what makes moving product images to a custom domain later a
    /// configuration change instead of a data migration.
    /// </summary>
    public interface IProductImageUrlResolver : IStoredImageUrlResolver
    {
        /// <summary>
        /// A new key for an image about to be written:
        /// businesses/{businessId}/products/{productId}/images/{imageId}.{extension}
        ///
        /// The image id is generated here and is never derived from client input.
        /// <paramref name="extension"/> is expected to include its leading dot and to
        /// have come from a verified byte signature rather than from a filename.
        /// </summary>
        string BuildKey(Guid businessId, Guid productId, string extension);

        /// <summary>
        /// Turns a value coming back from a client into the value to store, rejecting
        /// anything that does not belong to <paramref name="businessId"/>.
        ///
        /// This is an authorization check as much as a parse: without it a business
        /// can attach another business's image to its own product simply by sending
        /// that URL back.
        ///
        /// Throws InvalidProductImageException for anything unrecognised or foreign.
        /// </summary>
        string ToStorageKey(string incoming, Guid businessId);

    }
}
