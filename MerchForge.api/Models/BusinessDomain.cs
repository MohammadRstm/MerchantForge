namespace MerchForge.api.Models;

/// <summary>
/// A business vertical (Fashion, Restaurant, Electronics, ...). Owns the set of
/// categories businesses in that vertical can assign products to.
///
/// Named BusinessDomain rather than Domain deliberately: MerchForge will later gain
/// hostname/subdomain-based storefront resolution, where "domain" means a DNS name.
/// Keeping these distinct now avoids an ambiguous rename across the database, API,
/// SDK, and every independent storefront later. The public API still exposes this as
/// "domain", since that is the product-level term.
/// </summary>
public class BusinessDomain
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Stable, human-readable public identifier ("fashion"). Storefronts and the SDK
    /// can branch on this without depending on a Guid or a display name that may be
    /// renamed or localized.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Lets SuperAdmins retire a domain without deleting it and orphaning the
    /// categories/businesses that reference it.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public ICollection<Category> Categories { get; set; }
        = new List<Category>();

    public ICollection<Business> Businesses { get; set; }
        = new List<Business>();

    /// <summary>The product metadata fields businesses in this domain may opt into.</summary>
    public ICollection<ProductAttributeDefinition> ProductAttributeDefinitions { get; set; }
        = new List<ProductAttributeDefinition>();
}
