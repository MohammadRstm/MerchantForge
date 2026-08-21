namespace MerchForge.api.Models;

/// <summary>
/// One image belonging to a product. A product can have several — the gallery — with
/// exactly one marked <see cref="IsMain"/> as the card/thumbnail image.
///
/// Deliberately a separate table rather than a JSON array on Product: width/height
/// need to be genuine typed columns (not re-parsed on every read), gallery order needs
/// a real sort key, and "exactly one main image" is an invariant the database can
/// enforce here in a way a JSON blob never could.
///
/// Product.ImageUrl is untouched by this table for now — this is additive, not a
/// replacement. Whether ImageUrl becomes a computed/derived view over
/// Images.Single(i => i.IsMain) is a follow-up once the consumers of ImageUrl are
/// ready to move.
/// </summary>
public class ProductImage
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// True for the one image used as the product's card/thumbnail. Enforced as
    /// at-most-one-per-product by a database constraint (see
    /// ProductImageConfiguration), not just application code.
    /// </summary>
    public bool IsMain { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    /// <summary>Accessibility text. Optional — falls back to the product title if null.</summary>
    public string? AltText { get; set; }

    /// <summary>Gallery sort position, lowest first. Not required to be contiguous.</summary>
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public Product Product { get; set; } = null!;
}
