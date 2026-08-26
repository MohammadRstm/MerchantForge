namespace MerchForge.api.Configurations;

public class WebsiteTemplateVideoOptions
{
    public const string SectionName = "WebsiteTemplateVideos";

    /// <summary>Folder under the web root that uploaded template preview videos are written to.</summary>
    public string RelativePath { get; set; } = "uploads/website-templates";

    /// <summary>
    /// Cap on a single upload. Enforced in the service as well as by the request body
    /// limit, so an oversized file is rejected with a clear error rather than a
    /// generic 413 from the framework. Videos are much larger than product photos,
    /// hence the much higher default than ProductImageOptions.
    /// </summary>
    public long MaxBytes { get; set; } = 200 * 1024 * 1024;
}
