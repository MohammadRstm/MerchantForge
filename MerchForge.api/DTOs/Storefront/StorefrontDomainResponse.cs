namespace MerchForge.api.DTOs.Storefront;

/// <summary>
/// The business vertical a storefront operates in. Exposed as "domain" because that
/// is the product-level term; the entity is called BusinessDomain internally to keep
/// it distinct from DNS domains.
/// </summary>
public class StorefrontDomainResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Stable public identifier ("fashion"). Storefronts should branch on this rather
    /// than on Name, which is a display value.
    /// </summary>
    public string Slug { get; set; } = string.Empty;
}
