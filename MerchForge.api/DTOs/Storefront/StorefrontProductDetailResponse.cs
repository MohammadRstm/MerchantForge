using System.Text.Json;

namespace MerchForge.api.DTOs.Storefront;

/// <summary>
/// A single product with its full description. Same shape as
/// <see cref="StorefrontProductResponse"/> plus Description, so a storefront can
/// reuse most of its rendering between grid and detail views.
/// </summary>
public class StorefrontProductDetailResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public StorefrontProductCategoryResponse Category { get; set; } = new();

    public JsonDocument? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }
}
