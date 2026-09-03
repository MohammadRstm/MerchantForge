using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.BusinessDashboard;
using MerchForge.api.Exceptions.Storage;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.BusinessDashboard.interfaces;
using MerchForge.api.Services.Storage.interfaces;
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
        /// stored under an image extension and later served back as an image.
        ///
        /// It matters more since the move to object storage, not less. Files on the
        /// API's own origin also got X-Content-Type-Options and a locked-down CSP from
        /// the static-file handler as a second, independent layer; a public bucket
        /// sends neither, so the content type written on the object - which is the
        /// verified one from this table, never the client's claim - is what a browser
        /// will trust.
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
        private readonly IObjectStorage _objectStorage;
        private readonly IProductImageUrlResolver _urlResolver;
        private readonly IBusinessDashboardRepository _businessDashboardRepository;
        private readonly ILogger<ProductImageService> _logger;

        public ProductImageService(
            IOptions<ProductImageOptions> options,
            IWebHostEnvironment environment,
            IObjectStorage objectStorage,
            IProductImageUrlResolver urlResolver,
            IBusinessDashboardRepository businessDashboardRepository,
            ILogger<ProductImageService> logger)
        {
            _options = options.Value;
            _environment = environment;
            _objectStorage = objectStorage;
            _urlResolver = urlResolver;
            _businessDashboardRepository = businessDashboardRepository;
            _logger = logger;
        }

        public async Task<string> SaveAsync(
            Guid businessId,
            Guid productId,
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

            var verified = await ResolveVerifiedTypeAsync(file, cancellationToken);

            await EnsureProductIsAvailableToBusinessAsync(businessId, productId, cancellationToken);

            await using var stream = file.OpenReadStream();

            return await WriteAsync(businessId, productId, stream, verified, cancellationToken);
        }

        public async Task<string> SaveAsync(
            Guid businessId,
            Guid productId,
            byte[] bytes,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            if (bytes.Length == 0)
            {
                throw new InvalidProductImageException("The image is empty.");
            }

            if (bytes.Length > _options.MaxBytes)
            {
                var maxMb = _options.MaxBytes / (1024 * 1024);
                throw new InvalidProductImageException($"Images must be {maxMb} MB or smaller.");
            }

            var verified = ResolveVerifiedType(contentType, bytes);

            await EnsureProductIsAvailableToBusinessAsync(businessId, productId, cancellationToken);

            using var stream = new MemoryStream(bytes, writable: false);

            return await WriteAsync(businessId, productId, stream, verified, cancellationToken);
        }

        public async Task<(byte[] Bytes, string ContentType)> ReadAsync(
            Guid businessId,
            string storedValue,
            CancellationToken cancellationToken = default)
        {
            // The ownership check. ToStorageKey rejects anything that is not this
            // business's, in either the object-key or the pre-migration shape, and
            // gives a foreign value the same answer as a malformed one.
            var key = _urlResolver.ToStorageKey(storedValue, businessId);

            if (_urlResolver.IsLegacyLocalPath(key))
            {
                return await ReadFromDiskAsync(key, cancellationToken);
            }

            try
            {
                return await _objectStorage.GetAsync(key, cancellationToken);
            }
            catch (ObjectNotFoundException)
            {
                // Same answer a missing file has always given. A bucket that is
                // unreachable is a different matter and is left to propagate.
                throw new InvalidProductImageException("That image could not be found.");
            }
        }

        public async Task DeleteManyAsync(
            Guid businessId,
            IReadOnlyCollection<string> storedValues,
            CancellationToken cancellationToken = default)
        {
            var keys = new List<string>(storedValues.Count);

            foreach (var storedValue in storedValues)
            {
                // Pre-migration images are deliberately left on disk: they were never
                // cleaned up before, and removing them is a separate, deliberate step.
                if (_urlResolver.IsLegacyLocalPath(storedValue))
                {
                    continue;
                }

                try
                {
                    keys.Add(_urlResolver.ToStorageKey(storedValue, businessId));
                }
                catch (InvalidProductImageException)
                {
                    // Cleanup is not the place to fail a delete over an unrecognised
                    // stored value; skipping it leaves an orphan, which is the same
                    // outcome the app had before it deleted anything at all.
                    _logger.LogWarning(
                        "Skipped cleanup of an unrecognised image reference on business {BusinessId}.",
                        businessId);
                }
            }

            if (keys.Count == 0)
            {
                return;
            }

            try
            {
                await _objectStorage.DeleteManyAsync(keys, cancellationToken);
            }
            catch (ObjectStorageException exception)
            {
                // The rows are already gone and the caller has been told the delete
                // succeeded, so there is nothing useful to surface. An orphaned object
                // costs storage; a failure here would cost correctness.
                _logger.LogWarning(
                    exception.InnerStorageException ?? exception,
                    "Could not remove {Count} orphaned image(s) for business {BusinessId}. {Message}",
                    keys.Count,
                    businessId,
                    exception.Message);
            }
        }

        private async Task<string> WriteAsync(
            Guid businessId,
            Guid productId,
            Stream content,
            (string ContentType, string Extension) verified,
            CancellationToken cancellationToken)
        {
            // The image id inside the key is generated here and never derived from
            // client input, which would otherwise allow overwriting another image.
            var key = _urlResolver.BuildKey(businessId, productId, verified.Extension);

            await _objectStorage.PutAsync(key, content, verified.ContentType, cancellationToken);

            return key;
        }

        /// <summary>
        /// A product id is allowed to be one that does not exist yet - images are
        /// uploaded before the product is committed - but never one that already
        /// belongs to somebody else, which would put a key under this business naming
        /// another business's product.
        ///
        /// This is not what prevents a cross-tenant write; businessId comes from the
        /// authorized route, so the key is inside this business's prefix whatever is
        /// passed here. It is what stops the hierarchy from lying.
        /// </summary>
        private async Task EnsureProductIsAvailableToBusinessAsync(
            Guid businessId,
            Guid productId,
            CancellationToken cancellationToken)
        {
            if (productId == Guid.Empty)
            {
                throw new InvalidProductImageException("A product is required to store an image against.");
            }

            var ownerBusinessId = await _businessDashboardRepository.GetProductOwnerBusinessIdAsync(
                productId,
                cancellationToken);

            if (ownerBusinessId is not null && ownerBusinessId != businessId)
            {
                throw new InvalidProductImageException("That product does not belong to this business.");
            }
        }

        /// <summary>
        /// Reads an image stored before the move to object storage. These are still
        /// files under the web root and are still served by the API itself, so nothing
        /// about them changed; only newly uploaded images go to the bucket.
        /// </summary>
        private async Task<(byte[] Bytes, string ContentType)> ReadFromDiskAsync(
            string relativePath,
            CancellationToken cancellationToken)
        {
            var absolutePath = Path.Combine(
                WebRootPath,
                relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!File.Exists(absolutePath))
            {
                throw new InvalidProductImageException("That image could not be found.");
            }

            var bytes = await File.ReadAllBytesAsync(absolutePath, cancellationToken);

            var extension = Path.GetExtension(absolutePath).ToLowerInvariant();
            var contentType = AllowedImages.FirstOrDefault(a => a.Extension == extension).ContentType
                ?? "application/octet-stream";

            return (bytes, contentType);
        }

        private string WebRootPath =>
            // WebRootPath is null when wwwroot doesn't exist yet on a fresh checkout.
            _environment.WebRootPath
            ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

        private static async Task<(string ContentType, string Extension)> ResolveVerifiedTypeAsync(
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

            if (!MatchesSignature(candidate, header))
            {
                // Deliberately does not echo the declared type back — the mismatch is
                // the whole finding, and restating the client's claim invites confusion.
                throw new InvalidProductImageException(
                    "The uploaded file isn't a valid image of the type it claims to be.");
            }

            return (candidate.ContentType, candidate.Extension);
        }

        private static (string ContentType, string Extension) ResolveVerifiedType(string contentType, byte[] bytes)
        {
            var declared = contentType?.ToLowerInvariant() ?? string.Empty;

            var candidate = AllowedImages.FirstOrDefault(a => a.ContentType == declared);

            if (candidate.ContentType is null)
            {
                throw new InvalidProductImageException("Images must be JPEG, PNG, GIF or WEBP.");
            }

            if (bytes.Length < 4)
            {
                throw new InvalidProductImageException("The image isn't a valid image.");
            }

            var header = bytes.Length >= 12 ? bytes[..12] : bytes;

            if (!MatchesSignature(candidate, header))
            {
                throw new InvalidProductImageException(
                    "The image isn't a valid image of the type it claims to be.");
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
