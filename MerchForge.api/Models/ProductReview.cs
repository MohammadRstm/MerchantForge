namespace MerchForge.api.Models;

/// <summary>
/// One customer's review of one product: a required 1-5 star rating plus an optional
/// comment. That shape is a platform-wide convention every storefront template
/// renders the same way, not a per-business setting.
///
/// Only a customer who has actually ordered this product can create one, so every row
/// here is a verified purchase by construction — there is deliberately no
/// IsVerifiedPurchase flag, because it could only ever be true.
///
/// The author's display name is not snapshotted. Unlike Order, which freezes its
/// customer details because it is a financial record, a review only ever needs the
/// name for display, so it is projected from Customer at read time. That keeps the
/// customer's name in one place and lets the display rule (currently first name plus
/// last initial) change without a migration.
/// </summary>
public class ProductReview
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>Denormalized from Product.BusinessId so business-scoped moderation
    /// queries don't need to join through Product every time — same reasoning as
    /// StockMovement.BusinessId. A product never moves between businesses, so this
    /// can't drift.</summary>
    public Guid BusinessId { get; set; }

    public Guid CustomerId { get; set; }

    /// <summary>1-5. Constrained by CK_product_reviews_Rating_Range in the database as
    /// well as by the request validator — a rating outside that range would silently
    /// corrupt every average that reads it.</summary>
    public int Rating { get; set; }

    /// <summary>Optional free-text. Null when the customer rated without writing
    /// anything, which is the common case.</summary>
    public string? Comment { get; set; }

    /// <summary>Hidden reviews stay in the table and stay visible to the business
    /// owner — hiding is not deleting. A hidden review is excluded from storefront
    /// reads and from the product's average rating.</summary>
    public bool IsHidden { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Moves when the customer edits their own review; a customer has at most
    /// one review per product, so editing updates this row rather than adding another.</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Customer Customer { get; set; } = null!;
}
