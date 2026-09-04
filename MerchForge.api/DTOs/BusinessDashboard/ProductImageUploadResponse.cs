namespace MerchForge.api.DTOs.BusinessDashboard;

public class ProductImageUploadResponse
{
    /// <summary>
    /// URL the stored image can be loaded from, and the value to send back when
    /// saving the product. The database holds the object key behind it; the URL is
    /// built at the API boundary so the delivery origin is never persisted.
    /// </summary>
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// Dimensions of the image as actually stored, which is not necessarily the file
    /// that was uploaded - oversized images are scaled down first. Measured here
    /// rather than in the browser for exactly that reason: the client only ever sees
    /// the original.
    ///
    /// Null when the image could not be decoded, which leaves the gallery without
    /// dimensions rather than recording wrong ones.
    /// </summary>
    public int? Width { get; set; }

    public int? Height { get; set; }
}
