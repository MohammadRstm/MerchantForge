using MerchForge.api.Enums;

namespace MerchForge.api.Models;

/// <summary>
/// A product metadata field a business in this domain may opt into — "colors" for
/// Fashion, "spicy" for Restaurant, "ram" for Electronics.
///
/// This is the platform's catalogue of allowed fields, not per-business
/// configuration. Businesses pick from it during onboarding and the selection is
/// snapshotted into Business.MetadataShape. Constraining the choice this way is the
/// point: letting each business invent arbitrary JSON keys would make every
/// product's metadata unqueryable and un-renderable across stores.
/// </summary>
public class ProductAttributeDefinition
{
    public Guid Id { get; set; }

    public Guid BusinessDomainId { get; set; }

    /// <summary>
    /// The JSON key this field occupies in Product.Metadata. Stable and
    /// machine-facing — renaming it would orphan existing product metadata.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Display label for the product form. Safe to change.</summary>
    public string Label { get; set; } = string.Empty;

    public ProductAttributeValueType ValueType { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>
    /// Lets a field be retired without deleting it, so businesses that already
    /// snapshotted it keep working.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public BusinessDomain BusinessDomain { get; set; } = null!;
}
