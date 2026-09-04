using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Services.Storage.interfaces;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Services.Storage
{
    public class ProductImageUrlResolver : StoredImageUrlResolver, IProductImageUrlResolver
    {
        private const string BusinessesSegment = "businesses";
        private const string ProductsSegment = "products";
        private const string ImagesSegment = "images";
        private const string LegacyImagesSegment = "legacy-images";

        /// <summary>
        /// Mirrors the extensions ProductImageService derives from a verified byte
        /// signature. Duplicated rather than shared because the signature table also
        /// carries the magic bytes themselves, which have no business being reachable
        /// from a URL parser. A key is only ever written by the service above, so this
        /// list is a shape check on the way back in, not the real gate.
        /// </summary>
        private static readonly string[] AllowedExtensions = [".jpg", ".png", ".gif", ".webp"];

        private readonly string _legacyRelativePath;

        public ProductImageUrlResolver(
            IOptions<R2Options> r2Options,
            IOptions<ProductImageOptions> productImageOptions)
            : base(r2Options)
        {
            _legacyRelativePath = productImageOptions.Value.RelativePath.Trim('/');
        }

        public string BuildKey(Guid businessId, Guid productId, string extension)
        {
            return $"{BusinessesSegment}/{businessId}/{ProductsSegment}/{productId}/{ImagesSegment}/{Guid.NewGuid()}{extension}";
        }

        public string ToStorageKey(string incoming, Guid businessId)
        {
            var value = incoming?.Trim() ?? string.Empty;

            if (value.Length == 0)
            {
                throw new InvalidProductImageException("An image reference is required.");
            }

            // Rejected before anything else is attempted: ".." is only ever an attempt
            // to climb out of the folder or prefix the rest of this method pins the
            // value to.
            if (value.Contains(".."))
            {
                throw NotThisBusiness();
            }

            // An absolute URL is reduced to its path, and only the path identifies the
            // object. Matching on the path shape rather than on the configured
            // PublicBaseUrl means pointing delivery at a custom domain later does not
            // strand URLs already sitting in an open browser form.
            // The leading-slash check comes first deliberately. On Unix, Uri.TryCreate
            // parses an absolute filesystem path as a file: URI and succeeds, so an
            // API-relative path would be rejected below as a foreign scheme before it
            // ever reached the branch meant to accept it. On Windows the same call
            // returns false, which is why this only shows up off a developer machine.
            if (!value.StartsWith('/')
                && Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                {
                    throw NotThisBusiness();
                }

                value = Uri.UnescapeDataString(uri.AbsolutePath);
            }

            // A pre-migration image, still on disk. Same prefix check ProductImageService
            // has always applied, so editing an old product keeps working unchanged.
            var legacyPrefix = $"/{_legacyRelativePath}/{businessId}/";

            if (value.StartsWith(legacyPrefix, StringComparison.Ordinal))
            {
                return value;
            }

            var key = value.TrimStart('/');

            if (TryParseKey(key, out var keyBusinessId) && keyBusinessId == businessId)
            {
                return key;
            }

            throw NotThisBusiness();
        }

        /// <summary>
        /// Deliberately the same message whether the key was malformed or simply
        /// belonged to somebody else. Distinguishing them would let a caller probe
        /// which images exist under another business.
        /// </summary>
        private static InvalidProductImageException NotThisBusiness()
        {
            return new InvalidProductImageException("That image does not belong to this business.");
        }

        private static bool TryParseKey(string key, out Guid businessId)
        {
            businessId = Guid.Empty;

            var segments = key.Split('/');

            if (segments.Length < 2 || segments[0] != BusinessesSegment || !Guid.TryParse(segments[1], out businessId))
            {
                return false;
            }

            return segments.Length switch
            {
                // businesses/{businessId}/products/{productId}/images/{imageId}.{ext}
                6 => segments[2] == ProductsSegment
                    && segments[4] == ImagesSegment
                    && Guid.TryParse(segments[3], out _)
                    && IsImageFileName(segments[5]),

                // businesses/{businessId}/legacy-images/{imageId}.{ext} - images carried
                // over from the local-disk era that no product ever claimed, so there
                // was nothing truthful to nest them under. Nothing new is written here.
                4 => segments[2] == LegacyImagesSegment && IsImageFileName(segments[3]),

                _ => false,
            };
        }

        private static bool IsImageFileName(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            return AllowedExtensions.Contains(extension)
                && Guid.TryParse(Path.GetFileNameWithoutExtension(fileName), out _);
        }
    }
}
