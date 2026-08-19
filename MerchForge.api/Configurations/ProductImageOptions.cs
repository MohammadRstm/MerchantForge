namespace MerchForge.api.Configurations;

public class ProductImageOptions
{
    public const string SectionName = "ProductImages";

    /// <summary>Folder under the web root that uploaded product images are written to.</summary>
    public string RelativePath { get; set; } = "uploads/products";

    /// <summary>
    /// Cap on a single upload. Enforced in the service as well as by the request body
    /// limit, so an oversized file is rejected with a clear error rather than a
    /// generic 413 from the framework.
    /// </summary>
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;
}
