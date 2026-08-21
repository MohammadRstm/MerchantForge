using System.Text.Json;

namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// A single product as the merchant dashboard needs it — enough to populate the edit
/// form, so unlike the list response it carries description, categoryId and metadata.
/// </summary>
public class BusinessProductDetailResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal? CompareAtPrice { get; set; }

    /// <summary>The id, not the name: this populates a select in the edit form.</summary>
    public Guid CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    /// <summary>The main image's URL, kept in sync with Images — for consumers that only need one image.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>The full gallery, ordered for display. One entry has IsMain = true.</summary>
    public List<ProductImageResponse> Images { get; set; } = [];

    public string? Sku { get; set; }

    public int? StockQuantity { get; set; }

    public List<string> Tags { get; set; } = [];

    public DateTime? SaleEndsAt { get; set; }

    public JsonDocument? Metadata { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
