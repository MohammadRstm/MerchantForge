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

    public DateTime CreatedAt { get; set; }
}
