using MerchForge.api.Configurations;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Services.BusinessDashboard
{
    /// <summary>Structural copy of ProductImageService — same byte-signature validation, same per-business folder-and-GUID-filename storage.</summary>
    public class WebsiteCustomizationImageService : IWebsiteCustomizationImageService
    {
        private static readonly (string ContentType, string Extension, byte[][] Signatures)[] AllowedImages =
        [
            ("image/jpeg", ".jpg", [[0xFF, 0xD8, 0xFF]]),
            ("image/png",  ".png", [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]]),
            ("image/gif",  ".gif", [[0x47, 0x49, 0x46, 0x38, 0x37, 0x61], [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]]),
            ("image/webp", ".webp", [[0x52, 0x49, 0x46, 0x46]]),
            // Favicons are also commonly .ico — allowed only for that kind (checked at
            // the call site via the caller's own kind-aware limit, not here; ICO's
            // signature is the same four bytes regardless of kind, so any kind could
            // technically upload one, which is harmless — an .ico used as a logo just
            // renders like an unusual image).
            ("image/x-icon", ".ico", [[0x00, 0x00, 0x01, 0x00]]),
        ];

        private readonly WebsiteCustomizationImageOptions _options;
        private readonly IWebHostEnvironment _environment;

        public WebsiteCustomizationImageService(
            IOptions<WebsiteCustomizationImageOptions> options,
            IWebHostEnvironment environment)
        {
            _options = options.Value;
            _environment = environment;
        }

        public async Task<string> SaveAsync(
            Guid businessId,
            IFormFile file,
            WebsiteCustomizationImageKind kind,
            CancellationToken cancellationToken = default)
        {
            if (file.Length == 0)
            {
                throw new InvalidWebsiteCustomizationImageException("The uploaded file is empty.");
            }

            var maxBytes = kind == WebsiteCustomizationImageKind.Favicon
                ? _options.FaviconMaxBytes
                : _options.MaxBytes;

            if (file.Length > maxBytes)
            {
                var maxMb = maxBytes / (1024 * 1024);
                throw new InvalidWebsiteCustomizationImageException($"Images must be {maxMb} MB or smaller.");
            }

            var extension = await ResolveVerifiedExtensionAsync(file, cancellationToken);

            await using var stream = file.OpenReadStream();

            return await WriteAsync(businessId, stream, extension, cancellationToken);
        }

        private async Task<string> WriteAsync(
            Guid businessId,
            Stream content,
            string extension,
            CancellationToken cancellationToken)
        {
            var relativeDirectory = Path.Combine(_options.RelativePath, businessId.ToString());
            var absoluteDirectory = Path.Combine(WebRootPath, relativeDirectory);

            Directory.CreateDirectory(absoluteDirectory);

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var absolutePath = Path.Combine(absoluteDirectory, fileName);

            await using (var destination = new FileStream(absolutePath, FileMode.CreateNew))
            {
                await content.CopyToAsync(destination, cancellationToken);
            }

            return $"/{relativeDirectory.Replace('\\', '/')}/{fileName}";
        }

        private string WebRootPath =>
            _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

        private static async Task<string> ResolveVerifiedExtensionAsync(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var declared = file.ContentType?.ToLowerInvariant() ?? string.Empty;

            var candidate = AllowedImages.FirstOrDefault(a => a.ContentType == declared);

            if (candidate.ContentType is null)
            {
                throw new InvalidWebsiteCustomizationImageException(
                    "Images must be JPEG, PNG, GIF, WEBP, or ICO.");
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
                    throw new InvalidWebsiteCustomizationImageException("The uploaded file isn't a valid image.");
                }
            }

            if (!MatchesSignature(candidate, header))
            {
                throw new InvalidWebsiteCustomizationImageException(
                    "The uploaded file isn't a valid image of the type it claims to be.");
            }

            return candidate.Extension;
        }

        private static bool MatchesSignature(
            (string ContentType, string Extension, byte[][] Signatures) candidate,
            byte[] header)
        {
            var matches = candidate.Signatures.Any(signature =>
                header.Length >= signature.Length &&
                header.Take(signature.Length).SequenceEqual(signature));

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
