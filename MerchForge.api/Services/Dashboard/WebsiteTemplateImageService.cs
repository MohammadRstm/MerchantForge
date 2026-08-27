using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.Dashboard;
using MerchForge.api.Services.Dashboard.interfaces;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Services.Dashboard
{
    public class WebsiteTemplateImageService : IWebsiteTemplateImageService
    {
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

        private readonly WebsiteTemplateImageOptions _options;
        private readonly IWebHostEnvironment _environment;

        public WebsiteTemplateImageService(
            IOptions<WebsiteTemplateImageOptions> options,
            IWebHostEnvironment environment)
        {
            _options = options.Value;
            _environment = environment;
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

            var extension = await ResolveVerifiedExtensionAsync(file, cancellationToken);

            var relativeDirectory = _options.RelativePath;
            var absoluteDirectory = Path.Combine(WebRootPath, relativeDirectory);

            Directory.CreateDirectory(absoluteDirectory);

            // Filename is generated server-side and never derived from client input,
            // which would otherwise allow path traversal or overwriting another
            // template's file.
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var absolutePath = Path.Combine(absoluteDirectory, fileName);

            await using (var source = file.OpenReadStream())
            await using (var destination = new FileStream(absolutePath, FileMode.CreateNew))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            // Forward slashes: this becomes a URL, not a filesystem path.
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

            return candidate.Extension;
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
