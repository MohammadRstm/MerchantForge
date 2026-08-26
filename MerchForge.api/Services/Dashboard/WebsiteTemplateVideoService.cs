using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.Dashboard;
using MerchForge.api.Services.Dashboard.interfaces;
using Microsoft.Extensions.Options;

namespace MerchForge.api.Services.Dashboard
{
    public class WebsiteTemplateVideoService : IWebsiteTemplateVideoService
    {
        /// <summary>
        /// Allowed video types, each with the byte signature its files must actually
        /// start with -- same reasoning as ProductImageService: the declared content
        /// type and filename extension are both attacker-controlled.
        ///
        /// MP4 (and MOV, which shares the same ISO base media container) start with a
        /// 4-byte box size followed by the ASCII box type "ftyp" at offset 4. WEBM is
        /// an EBML document and always starts with the fixed 4-byte EBML header.
        /// </summary>
        private static readonly (string ContentType, string Extension, int SignatureOffset, byte[] Signature)[] AllowedVideos =
        [
            ("video/mp4", ".mp4", 4, [0x66, 0x74, 0x79, 0x70]),
            ("video/webm", ".webm", 0, [0x1A, 0x45, 0xDF, 0xA3]),
        ];

        private readonly WebsiteTemplateVideoOptions _options;
        private readonly IWebHostEnvironment _environment;

        public WebsiteTemplateVideoService(
            IOptions<WebsiteTemplateVideoOptions> options,
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
                throw new InvalidWebsiteTemplateVideoException("The uploaded file is empty.");
            }

            if (file.Length > _options.MaxBytes)
            {
                var maxMb = _options.MaxBytes / (1024 * 1024);
                throw new InvalidWebsiteTemplateVideoException($"Videos must be {maxMb} MB or smaller.");
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

            var candidate = AllowedVideos.FirstOrDefault(a => a.ContentType == declared);

            if (candidate.ContentType is null)
            {
                throw new InvalidWebsiteTemplateVideoException("Videos must be MP4 or WEBM.");
            }

            var headerLength = candidate.SignatureOffset + candidate.Signature.Length;
            var header = new byte[headerLength];

            await using (var stream = file.OpenReadStream())
            {
                var read = await stream.ReadAtLeastAsync(
                    header,
                    headerLength,
                    throwOnEndOfStream: false,
                    cancellationToken);

                if (read < headerLength)
                {
                    throw new InvalidWebsiteTemplateVideoException("The uploaded file isn't a valid video.");
                }
            }

            var matches = header
                .Skip(candidate.SignatureOffset)
                .Take(candidate.Signature.Length)
                .SequenceEqual(candidate.Signature);

            if (!matches)
            {
                // Deliberately does not echo the declared type back — the mismatch is
                // the whole finding, and restating the client's claim invites confusion.
                throw new InvalidWebsiteTemplateVideoException(
                    "The uploaded file isn't a valid video of the type it claims to be.");
            }

            return candidate.Extension;
        }
    }
}
