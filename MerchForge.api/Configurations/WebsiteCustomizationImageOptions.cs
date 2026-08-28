namespace MerchForge.api.Configurations;

public class WebsiteCustomizationImageOptions
{
    public const string SectionName = "WebsiteCustomizationImages";

    /// <summary>Folder under the web root that uploaded branding/template images are written to.</summary>
    public string RelativePath { get; set; } = "uploads/website-customization";

    /// <summary>Cap for a logo or template image upload.</summary>
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;

    /// <summary>Tighter cap for a favicon — it never needs to be large, and a small cap keeps a mistaken huge upload from silently becoming the site icon.</summary>
    public long FaviconMaxBytes { get; set; } = 1 * 1024 * 1024;
}
