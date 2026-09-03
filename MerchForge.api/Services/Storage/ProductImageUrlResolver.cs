using System.Diagnostics.CodeAnalysis;
using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Services.Storage.interfaces;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Services.Storage
{
    public class ProductImageUrlResolver : IProductImageUrlResolver
    {
        private const string BusinessesSegment = "businesses";
        private const string ProductsSegment = "products";
        private const string ImagesSegment = "images";

        /// <summary>
        /// Mirrors the extensions ProductImageService derives from a verified byte
        /// signature. Duplicated rather than shared because the signature table also
        /// carries the magic bytes themselves, which have no business being reachable
        /// from a URL parser. A key is only ever written by the service above, so this
        /// list is a shape check on the way back in, not the real gate.
        /// </summary>
        private static readonly string[] AllowedExtensions = [".jpg", ".png", ".gif", ".webp"];

        private readonly string _publicBaseUrl;
        private readonly string _legacyRelativePath;

        public ProductImageUrlResolver(
            IOptions<R2Options> r2Options,
            IOptions<ProductImageOptions> productImageOptions)
        {
            _publicBaseUrl = r2Options.Value.PublicBaseUrl.TrimEnd('/');
            _legacyRelativePath = productImageOptions.Value.RelativePath.Trim('/');
        }

        public string BuildKey(Guid businessId, Guid productId, string extension)
        {
            return $"{BusinessesSegment}/{businessId}/{ProductsSegment}/{productId}/{ImagesSegment}/{Guid.NewGuid()}{extension}";
        }

        [return: NotNullIfNotNull(nameof(storedValue))]
        public string? ToPublicUrl(string? storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue))
            {
                return storedValue;
            }

            // Images written before the R2 migration are still files under wwwroot and
            // are still served by the API itself, so their stored path is already what
            // the client needs.
            if (IsLegacyLocalPath(storedValue))
            {
                return storedValue;
            }

            // Already absolute. Nothing writes this today, but staying idempotent means
            // a projection that accidentally resolves twice produces the right answer
            // instead of a doubled origin.
            if (storedValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || storedValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return storedValue;
            }

            return $"{_publicBaseUrl}/{storedValue}";
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
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
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

        public bool IsLegacyLocalPath([NotNullWhen(true)] string? storedValue)
        {
            return storedValue is not null && storedValue.StartsWith('/');
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

            if (segments.Length != 6
                || segments[0] != BusinessesSegment
                || segments[2] != ProductsSegment
                || segments[4] != ImagesSegment)
            {
                return false;
            }

            if (!Guid.TryParse(segments[1], out businessId) || !Guid.TryParse(segments[3], out _))
            {
                return false;
            }

            var fileName = segments[5];
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            return AllowedExtensions.Contains(extension)
                && Guid.TryParse(Path.GetFileNameWithoutExtension(fileName), out _);
        }
    }
}
