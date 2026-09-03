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

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public decimal? CompareAtPrice { get; set; }

    public string? ImageUrl { get; set; }

    public List<StorefrontProductImageResponse> Images { get; set; } = [];

    public string? Sku { get; set; }

    public int? StockQuantity { get; set; }

    public List<string> Tags { get; set; } = [];

    public DateTime? SaleEndsAt { get; set; }

    public StorefrontProductCategoryResponse Category { get; set; } = new();

    public JsonDocument? Metadata { get; set; }


    /// <summary>
    /// Mean of this product's visible reviews, rounded to two places. Null when it has
    /// none — a real average can never be 0, so null is what distinguishes "not rated
    /// yet" from a genuinely bad score.
    /// </summary>
    public decimal? AverageRating { get; set; }

    /// <summary>How many visible reviews the average is drawn from. Hidden reviews
    /// count for nothing here.</summary>
    public int ReviewCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
