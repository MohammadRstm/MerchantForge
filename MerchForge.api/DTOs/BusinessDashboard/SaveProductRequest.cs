using System.Text.Json;

namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// Create/update payload for a product. One DTO for both: the fields a merchant can
/// set are identical, and splitting them would leave two shapes to keep in sync for
/// no behavioural difference.
/// </summary>
public class SaveProductRequest
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public Guid CategoryId { get; set; }

    /// <summary>
    /// Relative URL returned by the image upload endpoint, or null for no image.
    /// The file itself is uploaded separately so the form can show a preview before
    /// the product is saved.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Optional domain-specific fields, keyed by the attribute keys the business
    /// opted into. Left as raw JSON so each value can be validated against the type
    /// its definition declares rather than guessed at bind time.
    /// </summary>
    public Dictionary<string, JsonElement>? Metadata { get; set; }
}
