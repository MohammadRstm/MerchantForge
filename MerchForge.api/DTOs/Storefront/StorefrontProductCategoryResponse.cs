namespace MerchForge.api.DTOs.Storefront;

/// <summary>
/// The category a product belongs to, embedded in product responses so a storefront
/// can render "Shoes" on a product card without a second request or a client-side
/// join. Deliberately flat (no navigation back to the domain or its products) to
/// avoid circular serialization.
/// </summary>
public class StorefrontProductCategoryResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;
}
