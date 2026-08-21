namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// One image in a SaveProductRequest's Images list. Url must already exist — it's
/// whatever the image-upload endpoint returned for a prior upload of this file, not a
/// file itself.
/// </summary>
public class ProductImageRequest
{
    public string Url { get; set; } = string.Empty;

    /// <summary>Exactly one image per product must have this set — enforced both by the validator and a database constraint.</summary>
    public bool IsMain { get; set; }

    /// <summary>
    /// Optional pixel dimensions, when the client already knows them (e.g. read from
    /// the file before upload). Not verified against the actual file server-side.
    /// </summary>
    public int? Width { get; set; }

    public int? Height { get; set; }

    public string? AltText { get; set; }
}
