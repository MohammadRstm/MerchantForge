namespace MerchForge.api.DTOs.BusinessDashboard;

public class BusinessProductResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal? CompareAtPrice { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>Null means inventory isn't tracked for this product.</summary>
    public int? StockQuantity { get; set; }

    public string? Sku { get; set; }


    /// <summary>
    /// Mean of this product's reviews as shoppers see them — hidden reviews are
    /// excluded, so this matches the rating on the storefront exactly. Null when the
    /// product has no visible reviews; never 0, so "not rated yet" stays
    /// distinguishable from a genuinely low score.
    /// </summary>
    public decimal? AverageRating { get; set; }

    /// <summary>How many visible reviews the average is drawn from.</summary>
    public int ReviewCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
