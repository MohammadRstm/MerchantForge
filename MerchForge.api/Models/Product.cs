using System.Text.Json;

namespace MerchForge.api.Models;

public class Product
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>
    /// Domain-specific attributes that vary too much between verticals to be columns
    /// (fashion: colors/sizes/material; restaurant: ingredients/spicy; electronics:
    /// brand/storage/ram).
    ///
    /// Stored in a real `json` column, not a serialized string: on MariaDB that is
    /// LONGTEXT plus an automatic CHECK (json_valid(Metadata)) constraint, so the
    /// database rejects malformed JSON and server-side JSON functions work against
    /// it. Requires Pomelo's Json.Microsoft plugin (UseMicrosoftJson in Program.cs);
    /// without it EF cannot map JsonDocument at all.
    ///
    /// Intentionally unvalidated against any schema for now. A future
    /// ProductAttributeDefinition per domain/business can validate writes against
    /// this same column without a storage change.
    /// </summary>
    public JsonDocument? Metadata { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    public Business Business { get; set; } = null!;

    public Category Category { get; set; } = null!;
}
