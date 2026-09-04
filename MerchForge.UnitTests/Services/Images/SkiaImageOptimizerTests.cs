using FluentAssertions;
using MerchForge.api.Configurations;
using MerchForge.api.Services.Images;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace MerchForge.UnitTests.Services.Images;

/// <summary>
/// Exercised against genuinely encoded images rather than header stubs, because the
/// whole point of this class is what a decoder does with real pixels.
/// </summary>
public class SkiaImageOptimizerTests
{
    private static SkiaImageOptimizer Optimizer(int maxDimension = 2048, int quality = 80, bool enabled = true) =>
        new(
            Options.Create(new ImageOptimizationOptions
            {
                Enabled = enabled,
                MaxDimension = maxDimension,
                WebpQuality = quality,
            }),
            NullLogger<SkiaImageOptimizer>.Instance);

    /// <summary>
    /// Noise rather than a flat fill: a solid colour compresses to almost nothing in
    /// any format, which would make every size comparison meaningless.
    /// </summary>
    private static byte[] Photo(int width, int height, SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        using var bitmap = new SKBitmap(width, height);
        var random = new Random(42);

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                bitmap.SetPixel(x, y, new SKColor(
                    (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(format, 100);

        return data.ToArray();
    }

    private static (int Width, int Height) DimensionsOf(byte[] bytes)
    {
        using var codec = SKCodec.Create(new MemoryStream(bytes));
        return (codec!.Info.Width, codec.Info.Height);
    }

    [Fact]
    public void Scales_an_oversized_image_down_to_the_cap()
    {
        var result = Optimizer(maxDimension: 512).Optimize(Photo(1600, 900), "image/png", ".png");

        result.Width.Should().Be(512);
        result.Height.Should().Be(288, "the aspect ratio is preserved");
        DimensionsOf(result.Bytes).Should().Be((512, 288));
    }

    [Fact]
    public void Scales_by_the_longest_edge_whichever_way_round_the_image_is()
    {
        var result = Optimizer(maxDimension: 512).Optimize(Photo(900, 1600), "image/png", ".png");

        result.Height.Should().Be(512);
        result.Width.Should().Be(288);
    }

    [Fact]
    public void Leaves_an_image_already_within_the_cap_at_its_own_size()
    {
        var result = Optimizer(maxDimension: 2048).Optimize(Photo(800, 600), "image/png", ".png");

        result.Width.Should().Be(800);
        result.Height.Should().Be(600);
    }

    /// <summary>
    /// Enlarging a small image would add bytes and no detail.
    /// </summary>
    [Fact]
    public void Never_scales_an_image_up()
    {
        var result = Optimizer(maxDimension: 4000).Optimize(Photo(100, 100), "image/png", ".png");

        result.Width.Should().Be(100);
        result.Height.Should().Be(100);
    }

    [Fact]
    public void Converts_to_webp()
    {
        var result = Optimizer().Optimize(Photo(1200, 1200), "image/jpeg", ".jpg");

        result.ContentType.Should().Be("image/webp");
        result.Extension.Should().Be(".webp");
    }

    /// <summary>
    /// The reason any of this exists.
    ///
    /// The bound is deliberately loose because the fixture is random noise, which is
    /// the worst case for any compressor - there is no redundancy to exploit. Measured
    /// against a photo-like 4000x3000 JPEG the same settings took 2001 KB to 25 KB,
    /// so this assertion is a floor, not a target.
    /// </summary>
    [Fact]
    public void Substantially_reduces_the_stored_size_of_a_large_photo()
    {
        var original = Photo(3000, 2000);

        var result = Optimizer(maxDimension: 1024).Optimize(original, "image/png", ".png");

        result.Bytes.Length.Should().BeLessThan(original.Length / 2);
    }

    /// <summary>
    /// Growing a file in the name of shrinking it would be absurd. Small images can
    /// re-encode larger than they started, and those keep their original bytes.
    /// </summary>
    [Fact]
    public void Keeps_the_original_when_re_encoding_would_not_help()
    {
        var original = Photo(8, 8, SKEncodedImageFormat.Webp);

        var result = Optimizer().Optimize(original, "image/webp", ".webp");

        result.Bytes.Length.Should().BeLessThanOrEqualTo(original.Length);
    }

    [Fact]
    public void Reports_dimensions_even_when_it_keeps_the_original()
    {
        var result = Optimizer(enabled: false).Optimize(Photo(640, 480), "image/png", ".png");

        result.ContentType.Should().Be("image/png");
        result.Extension.Should().Be(".png");
        result.Bytes.Length.Should().Be(Photo(640, 480).Length);
    }

    [Fact]
    public void Disabled_stores_the_image_exactly_as_uploaded()
    {
        var original = Photo(3000, 2000);

        var result = Optimizer(enabled: false).Optimize(original, "image/png", ".png");

        result.Bytes.Should().BeSameAs(original);
        result.ContentType.Should().Be("image/png");
    }

    /// <summary>
    /// An upload that somehow reaches the decoder without being decodable must not
    /// cost the merchant their upload - it is stored as-is instead.
    /// </summary>
    [Fact]
    public void Undecodable_bytes_are_stored_unchanged_rather_than_rejected()
    {
        var garbage = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0xFF, 0xFF, 0xFF };

        var result = Optimizer().Optimize(garbage, "image/png", ".png");

        result.Bytes.Should().BeSameAs(garbage);
        result.ContentType.Should().Be("image/png");
        result.Extension.Should().Be(".png");
    }

    /// <summary>
    /// A still re-encode would silently throw away every frame but the first, turning
    /// a moving image into a static one.
    /// </summary>
    [Fact]
    public void Animated_images_are_left_alone()
    {
        var animatedGif = AnimatedGif();

        var result = Optimizer(maxDimension: 8).Optimize(animatedGif, "image/gif", ".gif");

        result.Bytes.Should().BeSameAs(animatedGif);
        result.Extension.Should().Be(".gif");
    }

    /// <summary>A hand-built two-frame GIF; Skia has no animated encoder.</summary>
    private static byte[] AnimatedGif()
    {
        var gif = new List<byte>();

        // Header, then a 2x2 logical screen with a two-colour global palette.
        gif.AddRange("GIF89a"u8.ToArray());
        gif.AddRange([0x02, 0x00, 0x02, 0x00, 0xF0, 0x00, 0x00]);
        gif.AddRange([0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF]);

        // Netscape looping extension, which is what marks it as an animation.
        gif.AddRange([0x21, 0xFF, 0x0B]);
        gif.AddRange("NETSCAPE2.0"u8.ToArray());
        gif.AddRange([0x03, 0x01, 0x00, 0x00, 0x00]);

        for (var frame = 0; frame < 2; frame++)
        {
            // Graphic control extension, then the image itself.
            gif.AddRange([0x21, 0xF9, 0x04, 0x00, 0x0A, 0x00, 0x00, 0x00]);
            gif.AddRange([0x2C, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x02, 0x00, 0x00]);
            gif.AddRange([0x02, 0x02, (byte)(0x44 + frame), 0x01, 0x00]);
        }

        gif.Add(0x3B);

        return gif.ToArray();
    }
}
