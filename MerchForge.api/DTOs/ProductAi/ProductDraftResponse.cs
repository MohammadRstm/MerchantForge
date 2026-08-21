using System.Text.Json;

namespace MerchForge.api.DTOs.ProductAi;

/// <summary>
/// The whole conversation as the client needs it. Every AI endpoint returns this
/// same shape, so the frontend has one thing to validate and render, and reacts to
/// backend state rather than tracking its own copy of the workflow.
/// </summary>
public class ProductDraftResponse
{
    public Guid Id { get; set; }

    /// <summary>Drives what the UI offers: keep chatting, approve an image, or confirm.</summary>
    public string Status { get; set; } = string.Empty;

    public List<ProductDraftMessageResponse> Messages { get; set; } = [];

    /// <summary>The product assembled so far. Null before the agent has extracted anything.</summary>
    public ProductDraftProductResponse? Draft { get; set; }

    /// <summary>Advisory, from the agent — the UI hints at these but doesn't gate on them.</summary>
    public List<string> MissingFields { get; set; } = [];

    public string? OriginalImageUrl { get; set; }

    public string? ProcessedImageUrl { get; set; }

    public string? ImageModificationPrompt { get; set; }

    /// <summary>
    /// Whether the backend considers the draft complete enough to create a product.
    /// Computed by validation, not taken from the agent's opinion — the confirm
    /// button is gated on this.
    /// </summary>
    public bool CanConfirm { get; set; }

    /// <summary>Set once confirmed, so the UI can link to the created product.</summary>
    public Guid? ProductId { get; set; }
}

public class ProductDraftMessageResponse
{
    public string Role { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    /// <summary>text | voice | image — lets the UI mark how a message arrived.</summary>
    public string Kind { get; set; } = "text";

    public DateTime At { get; set; }
}

public class ProductDraftProductResponse
{
    public string? Title { get; set; }

    public string? Description { get; set; }

    public decimal? Price { get; set; }

    /// <summary>Pre-discount price for a struck-through sale display. Null when not on sale.</summary>
    public decimal? CompareAtPrice { get; set; }

    public Guid? CategoryId { get; set; }

    /// <summary>Resolved for display, so the preview shows a name rather than a guid.</summary>
    public string? CategoryName { get; set; }

    public string? Sku { get; set; }

    /// <summary>Null means inventory isn't tracked for this product; 0 means tracked and out of stock.</summary>
    public int? StockQuantity { get; set; }

    /// <summary>Freeform merchandising badges, e.g. "New", "Bestseller".</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>When a time-limited sale on this product ends. Null for no promotion deadline.</summary>
    public DateTime? SaleEndsAt { get; set; }

    public JsonDocument? Metadata { get; set; }
}
