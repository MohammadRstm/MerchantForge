using System.Text.Json;

namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// Create/update payload for a product. One DTO for both: the fields a merchant can
/// set are identical, and splitting them would leave two shapes to keep in sync for
/// no behavioural difference.
/// </summary>
public class SaveProductRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    /// <summary>Pre-discount price for a struck-through sale display. Null when not on sale.</summary>
    public decimal? CompareAtPrice { get; set; }

    public Guid CategoryId { get; set; }

    /// <summary>
    /// 1 to 5 images, each already uploaded via the image-upload endpoint (that's a
    /// separate request so the form can preview a file before the product is saved).
    /// Exactly one must be flagged as the main image. Order in this list becomes
    /// gallery display order.
    /// </summary>
    public List<ProductImageRequest> Images { get; set; } = [];

    public string? Sku { get; set; }

    /// <summary>Null means inventory isn't tracked for this product; 0 means tracked and out of stock.</summary>
    public int? StockQuantity { get; set; }

    /// <summary>Freeform merchandising badges, e.g. "New", "Bestseller".</summary>
    public List<string>? Tags { get; set; }

    /// <summary>When a time-limited sale on this product ends. Null for no promotion deadline.</summary>
    public DateTime? SaleEndsAt { get; set; }

    /// <summary>
    /// Optional domain-specific fields, keyed by the attribute keys the business
    /// opted into. Left as raw JSON so each value can be validated against the type
    /// its definition declares rather than guessed at bind time.
    /// </summary>
    public Dictionary<string, JsonElement>? Metadata { get; set; }
}
