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

    public string? ImageUrl { get; set; }

    public StorefrontProductCategoryResponse Category { get; set; } = new();

    /// <summary>
    /// Free-form domain-specific attributes. Null when the product has none. The
    /// shape varies by business vertical and is not validated by the API yet.
    /// </summary>
    public JsonDocument? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }
}
