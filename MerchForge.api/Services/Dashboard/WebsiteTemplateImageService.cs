using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.Dashboard;
using MerchForge.api.Services.Dashboard.interfaces;
using MerchForge.api.Services.Storage.interfaces;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Services.Dashboard
{
    public class WebsiteTemplateImageService : IWebsiteTemplateImageService
    {
        /// <summary>
        /// Template previews are a global catalog with no owning business, so unlike a
        /// product image key there is nothing to scope them by.
        /// </summary>
        private const string KeyPrefix = "website-templates";

        /// <summary>
        /// Allowed image types, each with the byte signature(s) its files must actually
        /// start with -- same reasoning as ProductImageService: the declared content
        /// type and filename extension are both attacker-controlled, so neither proves
        /// what a file is.
        /// </summary>
        private static readonly (string ContentType, string Extension, byte[][] Signatures)[] AllowedImages =
        [
            ("image/jpeg", ".jpg", [[0xFF, 0xD8, 0xFF]]),
            ("image/png",  ".png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]]),
            ("image/gif",  ".gif", [[0x47, 0x49, 0x46, 0x38, 0x37, 0x61], [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]]),
            // WEBP is "RIFF"...."WEBP"; the first four bytes are enough to shortlist it,
            // with the WEBP marker at offset 8 confirmed separately below.
            ("image/webp", ".webp", [[0x52, 0x49, 0x46, 0x46]]),
        ];

        private static readonly string[] AllowedExtensions = [".jpg", ".png", ".gif", ".webp"];

        private readonly WebsiteTemplateImageOptions _options;
        private readonly IObjectStorage _objectStorage;
        private readonly IStoredImageUrlResolver _urlResolver;

        public WebsiteTemplateImageService(
            IOptions<WebsiteTemplateImageOptions> options,
            IObjectStorage objectStorage,
            IStoredImageUrlResolver urlResolver)
        {
            _options = options.Value;
            _objectStorage = objectStorage;
            _urlResolver = urlResolver;
        }

        public async Task<string> SaveAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file.Length == 0)
            {
                throw new InvalidWebsiteTemplateImageException("The uploaded file is empty.");
            }

            if (file.Length > _options.MaxBytes)
            {
                var maxMb = _options.MaxBytes / (1024 * 1024);
                throw new InvalidWebsiteTemplateImageException($"Images must be {maxMb} MB or smaller.");
            }

            var verified = await ResolveVerifiedTypeAsync(file, cancellationToken);

            // The image id is generated here and never derived from client input, which
            // would otherwise allow overwriting another template's preview.
            var key = $"{KeyPrefix}/{Guid.NewGuid()}{verified.Extension}";

            await using var source = file.OpenReadStream();

            // The verified content type, never the client's claim: a public bucket sends
            // no nosniff header, so this is what a browser will trust.
            await _objectStorage.PutAsync(key, source, verified.ContentType, cancellationToken);

            return key;
        }

        public string ToStorageKey(string incoming)
        {
            var value = incoming?.Trim() ?? string.Empty;

            if (value.Length == 0)
            {
                throw new InvalidWebsiteTemplateImageException("A preview image is required.");
            }

            if (value.Contains(".."))
            {
                throw NotAPreviewImage();
            }

            // An absolute URL is reduced to its path; only the path identifies the
            // object. Matching the shape rather than the configured public origin means
            // a URL issued before a move to a custom domain still resolves afterwards.
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
                {
                    throw NotAPreviewImage();
                }

                value = Uri.UnescapeDataString(uri.AbsolutePath);
            }

            // Anything still on local disk is kept verbatim. That covers previews
            // uploaded before the move, and the seeded /images/templates/coming-soon.jpg
            // placeholder, which is a bundled asset rather than an upload.
            if (_urlResolver.IsLegacyLocalPath(value))
            {
                return value;
            }

            var key = value.TrimStart('/');

            return IsTemplateKey(key) ? key : throw NotAPreviewImage();
        }

        /// <summary>
        /// Deliberately the same message whether the value was malformed or simply is
        /// not a template preview, so the endpoint cannot be used to probe storage.
        /// </summary>
        private static InvalidWebsiteTemplateImageException NotAPreviewImage()
        {
            return new InvalidWebsiteTemplateImageException("That is not a valid preview image.");
        }

        private static bool IsTemplateKey(string key)
        {
            var segments = key.Split('/');

            if (segments.Length != 2 || segments[0] != KeyPrefix)
            {
                return false;
            }

            var extension = Path.GetExtension(segments[1]).ToLowerInvariant();

            return AllowedExtensions.Contains(extension)
                && Guid.TryParse(Path.GetFileNameWithoutExtension(segments[1]), out _);
        }

        private static async Task<(string ContentType, string Extension)> ResolveVerifiedTypeAsync(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var declared = file.ContentType?.ToLowerInvariant() ?? string.Empty;

            var candidate = AllowedImages.FirstOrDefault(a => a.ContentType == declared);

            if (candidate.ContentType is null)
            {
                throw new InvalidWebsiteTemplateImageException("Images must be JPEG, PNG, GIF or WEBP.");
            }

            var header = new byte[12];

            await using (var stream = file.OpenReadStream())
            {
                var read = await stream.ReadAtLeastAsync(
                    header,
                    header.Length,
                    throwOnEndOfStream: false,
                    cancellationToken);

                if (read < 4)
                {
                    throw new InvalidWebsiteTemplateImageException("The uploaded file isn't a valid image.");
                }
            }

            if (!MatchesSignature(candidate, header))
            {
                // Deliberately does not echo the declared type back — the mismatch is
                // the whole finding, and restating the client's claim invites confusion.
                throw new InvalidWebsiteTemplateImageException(
                    "The uploaded file isn't a valid image of the type it claims to be.");
            }

            return (candidate.ContentType, candidate.Extension);
        }

        private static bool MatchesSignature(
            (string ContentType, string Extension, byte[][] Signatures) candidate,
            byte[] header)
        {
            var matches = candidate.Signatures.Any(signature =>
                header.Length >= signature.Length &&
                header.Take(signature.Length).SequenceEqual(signature));

            // RIFF alone is also AVI/WAV, so require the WEBP marker at offset 8.
            if (matches && candidate.Extension == ".webp")
            {
                matches = header.Length >= 12
                    && header[8] == 0x57 && header[9] == 0x45
                    && header[10] == 0x42 && header[11] == 0x50;
            }

            return matches;
        }
    }
}
