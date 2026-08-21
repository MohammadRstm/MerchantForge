using System.Text.Json;

namespace MerchForge.api.DTOs.Storefront;

/// <summary>
/// A product as it appears in a catalog listing.
///
/// Carries Metadata but not Description: product grids routinely need metadata to
/// render (colour swatches, size badges, a "spicy" marker), and omitting it would
/// force a detail request per card. Description is the genuinely large field and is
/// only returned by the detail endpoint.
///
/// BusinessId is absent by design — every storefront request is already scoped to
/// one business, so echoing it back is redundant. UpdatedAt is internal audit data
/// and is not exposed.
/// </summary>
public class StorefrontProductResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public decimal Price { get; set; }

    /// <summary>Pre-discount price for a struck-through sale display. Null when not on sale.</summary>
    public decimal? CompareAtPrice { get; set; }

    /// <summary>The main image's URL, kept in sync with Images — for consumers that only need one image.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>The full gallery, ordered for display. One entry has IsMain = true.</summary>
    public List<StorefrontProductImageResponse> Images { get; set; } = [];

    public string? Sku { get; set; }

    /// <summary>Null means inventory isn't tracked for this product; 0 means tracked and out of stock.</summary>
    public int? StockQuantity { get; set; }

    /// <summary>Freeform merchandising badges, e.g. "New", "Bestseller". Never null; empty when none.</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>When a time-limited sale on this product ends, for a countdown display. Null if none.</summary>
    public DateTime? SaleEndsAt { get; set; }

    public StorefrontProductCategoryResponse Category { get; set; } = new();

    /// <summary>
    /// Free-form domain-specific attributes. Null when the product has none. The
    /// shape varies by business vertical and is not validated by the API yet.
    /// </summary>
    public JsonDocument? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }
}
