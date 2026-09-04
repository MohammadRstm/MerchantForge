using MerchForge.api.Configurations;
using MerchForge.api.Services.Images.interfaces;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace MerchForge.api.Services.Images
{
    public class SkiaImageOptimizer : IImageOptimizer
    {
        private const string WebpContentType = "image/webp";
        private const string WebpExtension = ".webp";

        private readonly ImageOptimizationOptions _options;
        private readonly ILogger<SkiaImageOptimizer> _logger;

        public SkiaImageOptimizer(
            IOptions<ImageOptimizationOptions> options,
            ILogger<SkiaImageOptimizer> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public OptimizedImage Optimize(byte[] bytes, string contentType, string extension)
        {
            OptimizedImage Unchanged(int? width = null, int? height = null) =>
                new(bytes, contentType, extension, width, height);

            if (!_options.Enabled)
            {
                return Unchanged();
            }

            try
            {
                using var codec = SKCodec.Create(new MemoryStream(bytes, writable: false));

                if (codec is null)
                {
                    // Signature validation already said this is an image of an allowed
                    // type, so a decode failure here is surprising. Storing it as-is is
                    // still better than rejecting an upload the user was told was fine.
                    _logger.LogWarning("Could not decode an image for optimization; storing it unchanged.");
                    return Unchanged();
                }

                // An animated GIF has frames a still re-encode would silently throw
                // away, turning a moving image into a single frame. Left alone.
                if (codec.FrameCount > 1)
                {
                    return Unchanged(codec.Info.Width, codec.Info.Height);
                }

                using var decoded = SKBitmap.Decode(codec);

                if (decoded is null)
                {
                    _logger.LogWarning("Could not decode an image for optimization; storing it unchanged.");
                    return Unchanged();
                }

                using var resized = Resize(decoded);
                var source = resized ?? decoded;

                using var image = SKImage.FromBitmap(source);
                using var encoded = image.Encode(SKEncodedImageFormat.Webp, _options.WebpQuality);

                if (encoded is null)
                {
                    return Unchanged(decoded.Width, decoded.Height);
                }

                var optimized = encoded.ToArray();

                // Small or already-efficient images can come out bigger after a
                // re-encode. Growing a file in the name of shrinking it would be
                // absurd, so the original wins - but the dimensions are still worth
                // reporting, since they were measured properly here.
                if (optimized.Length >= bytes.Length && resized is null)
                {
                    return Unchanged(decoded.Width, decoded.Height);
                }

                return new OptimizedImage(optimized, WebpContentType, WebpExtension, source.Width, source.Height);
            }
            catch (Exception exception)
            {
                // Optimization is an improvement, not a requirement. A decoder fault
                // must never cost the merchant their upload.
                _logger.LogWarning(exception, "Image optimization failed; storing the image unchanged.");
                return Unchanged();
            }
        }

        /// <summary>
        /// Scales down so the longest edge fits MaxDimension, preserving aspect ratio.
        /// Returns null when the image is already small enough, so the caller can tell
        /// "not resized" from "resized" without comparing dimensions.
        ///
        /// Only ever scales down. Enlarging a small image would add bytes and no
        /// detail.
        /// </summary>
        private SKBitmap? Resize(SKBitmap source)
        {
            var longestEdge = Math.Max(source.Width, source.Height);

            if (longestEdge <= _options.MaxDimension)
            {
                return null;
            }

            var scale = (double)_options.MaxDimension / longestEdge;

            var width = Math.Max(1, (int)Math.Round(source.Width * scale));
            var height = Math.Max(1, (int)Math.Round(source.Height * scale));

            return source.Resize(new SKImageInfo(width, height), new SKSamplingOptions(SKCubicResampler.Mitchell));
        }
    }
}
