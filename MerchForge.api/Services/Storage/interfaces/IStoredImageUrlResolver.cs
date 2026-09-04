using System.Diagnostics.CodeAnalysis;

namespace MerchForge.api.Services.Storage.interfaces
{
    /// <summary>
    /// The half of the key/URL boundary that is the same for every kind of image:
    /// turning a stored value into something a browser can load, and telling an object
    /// key apart from a file still sitting on the API's own disk.
    ///
    /// Key construction is deliberately not here. What a valid key looks like differs
    /// per image kind - product images are nested under a business, template previews
    /// are global - so each kind owns its own format.
    /// </summary>
    public interface IStoredImageUrlResolver
    {
        /// <summary>
        /// Turns a stored value into something an img tag can load.
        ///
        /// Idempotent, and a no-op for the API-relative paths of images still on local
        /// disk, so applying it twice or to a pre-migration image is harmless.
        /// </summary>
        [return: NotNullIfNotNull(nameof(storedValue))]
        string? ToPublicUrl(string? storedValue);

        /// <summary>
        /// Whether a stored value refers to a file on the API's own disk rather than to
        /// an object in the bucket. Callers that clean up storage use this to leave
        /// pre-migration images alone.
        /// </summary>
        bool IsLegacyLocalPath([NotNullWhen(true)] string? storedValue);
    }
}
