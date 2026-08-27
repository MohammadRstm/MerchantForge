namespace MerchForge.api.Configurations;

public class WebsiteTemplateImageOptions
{
    public const string SectionName = "WebsiteTemplateImages";

    /// <summary>Folder under the web root that uploaded template preview images are written to.</summary>
    public string RelativePath { get; set; } = "uploads/website-templates";

    /// <summary>
    /// Cap on a single upload. Enforced in the service as well as by the request body
    /// limit, so an oversized file is rejected with a clear error rather than a
    /// generic 413 from the framework.
    /// </summary>
    public long MaxBytes { get; set; } = 10 * 1024 * 1024;
}
