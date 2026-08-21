using System.Text.Json;

namespace MerchForge.api.Models;

public class Product
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    /// <summary>
    /// The pre-discount price shown struck through next to <see cref="Price"/> (e.g.
    /// "$130 → $100"). Null when the product isn't on sale. Deliberately its own
    /// column rather than a derived "20% off" label: storing two numbers keeps the
    /// discount math in one place instead of a merchant having to keep a percentage
    /// text field in sync with the actual price by hand.
    /// </summary>
    public decimal? CompareAtPrice { get; set; }

    public string? ImageUrl { get; set; }

    /// <summary>
    /// Stock keeping unit — an operational/inventory code, not shown to customers by
    /// every storefront. Optional and unvalidated in format (merchants bring their
    /// own scheme); uniqueness is enforced per business, not platform-wide.
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Units available. Null means "not tracked" (the merchant doesn't manage
    /// inventory for this product) and is distinct from 0, which means "tracked and
    /// out of stock" — collapsing the two would make an intentionally untracked
    /// product look perpetually sold out.
    /// </summary>
    public int? StockQuantity { get; set; }

    /// <summary>
    /// Freeform merchandising badges ("New", "Bestseller", "Limited Edition").
    /// Unlike <see cref="Metadata"/> this is domain-agnostic — every vertical wants
    /// short promotional labels on a card, so it is a fixed column rather than
    /// something each business opts into. Never null; an empty list means no tags.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// When a time-limited sale on this product ends, for a "deal ends in..."
    /// countdown. Null means there's no active promotion deadline. Deliberately not
    /// validated against <see cref="CompareAtPrice"/> being set — a business may want
    /// to countdown a limited window even without a struck-through price.
    /// </summary>
    public DateTime? SaleEndsAt { get; set; }

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

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
