namespace MerchForge.api.Services.Dashboard.interfaces
{
    public interface IWebsiteTemplateImageService
    {
        /// <summary>
        /// Validates and stores an uploaded template preview image, returning the
        /// object key to save on the template. Throws InvalidWebsiteTemplateImageException
        /// when the file is empty, too large, or not genuinely an image of an allowed type.
        ///
        /// Unlike product images these are not scoped to a business - the template
        /// catalog is global and only a SuperAdmin can write to it.
        /// </summary>
        Task<string> SaveAsync(
            IFormFile file,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Turns a preview image reference coming back from a client into the value to
        /// store, accepting an object key, its public URL, or the API-relative path of
        /// an image stored before the move to object storage.
        ///
        /// Rejects anything else, so a template row cannot be pointed at an arbitrary
        /// URL on someone else's origin.
        /// </summary>
        string ToStorageKey(string incoming);
    }
}
