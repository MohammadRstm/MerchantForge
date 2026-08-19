using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Services.BusinessDashboard
{
    public class ProductImageService : IProductImageService
    {
        /// <summary>
        /// Allowed image types, each with the byte signature its files must actually
        /// start with.
        ///
        /// The signature check is the point: the browser-supplied content type and the
        /// filename extension are both attacker-controlled, so neither proves what a
        /// file is. Checking the real leading bytes stops a script or executable being
        /// stored under an image extension and later served back from our own origin.
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

        private readonly ProductImageOptions _options;
        private readonly IWebHostEnvironment _environment;

        public ProductImageService(
            IOptions<ProductImageOptions> options,
            IWebHostEnvironment environment)
        {
            _options = options.Value;
            _environment = environment;
        }

        public async Task<string> SaveAsync(
            Guid businessId,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file.Length == 0)
            {
                throw new InvalidProductImageException("The uploaded file is empty.");
            }

            if (file.Length > _options.MaxBytes)
            {
                var maxMb = _options.MaxBytes / (1024 * 1024);
                throw new InvalidProductImageException($"Images must be {maxMb} MB or smaller.");
            }

            var extension = await ResolveVerifiedExtensionAsync(file, cancellationToken);

            // Images are grouped per business so one business's uploads are never
            // interleaved with another's on disk.
            var relativeDirectory = Path.Combine(_options.RelativePath, businessId.ToString());
            var absoluteDirectory = Path.Combine(WebRootPath, relativeDirectory);

            Directory.CreateDirectory(absoluteDirectory);

            // Filename is generated server-side and never derived from the client's,
            // which would otherwise allow path traversal (../../appsettings.json) or
            // overwriting another business's file.
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var absolutePath = Path.Combine(absoluteDirectory, fileName);

            await using (var stream = new FileStream(absolutePath, FileMode.CreateNew))
            {
                await file.CopyToAsync(stream, cancellationToken);
            }

            // Forward slashes: this becomes a URL, not a filesystem path.
            return $"/{relativeDirectory.Replace('\\', '/')}/{fileName}";
        }

        private string WebRootPath =>
            // WebRootPath is null when wwwroot doesn't exist yet on a fresh checkout.
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
                throw new InvalidProductImageException(
                    "Images must be JPEG, PNG, GIF or WEBP.");
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
                    throw new InvalidProductImageException("The uploaded file isn't a valid image.");
                }
            }

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

            if (!matches)
            {
                // Deliberately does not echo the declared type back — the mismatch is
                // the whole finding, and restating the client's claim invites confusion.
                throw new InvalidProductImageException(
                    "The uploaded file isn't a valid image of the type it claims to be.");
            }

            return candidate.Extension;
        }
    }
}
