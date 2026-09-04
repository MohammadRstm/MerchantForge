namespace MerchForge.api.Configurations;

/// <summary>
/// How aggressively uploaded images are shrunk before they are stored.
///
/// The saving comes almost entirely from dimensions rather than encoder quality:
/// a phone photo arrives several thousand pixels wide, and no storefront renders
/// it at more than a fraction of that. Capping the longest edge and re-encoding to
/// WebP typically takes a 4 MB upload under 300 KB.
/// </summary>
public class ImageOptimizationOptions
{
    public const string SectionName = "ImageOptimization";

    /// <summary>
    /// Off means images are stored exactly as uploaded. Kept as a switch because
    /// re-encoding is lossy and irreversible once the original is discarded.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Longest edge, in pixels. Images at or under this are not resized, only
    /// re-encoded. 2048 is comfortably above what any current template renders,
    /// including a full-width hero on a high-density display.
    /// </summary>
    public int MaxDimension { get; set; } = 2048;

    /// <summary>
    /// WebP encoder quality, 1-100. 80 is the usual sweet spot: visually hard to
    /// separate from the original on photographic content, at a fraction of the
    /// size.
    /// </summary>
    public int WebpQuality { get; set; } = 80;
}
