namespace MerchForge.api.DTOs.Storefront;

/// <summary>
/// One review as a storefront visitor sees it.
///
/// CustomerId is absent by design. This is a public, anonymous-readable surface, and
/// exposing a stable per-customer identifier here would let anyone correlate one
/// person's reviews across every product in the store. AuthorDisplayName is a derived
/// label (first name plus last initial), never the customer's email.
/// </summary>
public class StorefrontProductReviewResponse
{
    public Guid Id { get; set; }

    public int Rating { get; set; }

    /// <summary>Null when the customer rated without writing anything.</summary>
    public string? Comment { get; set; }

    /// <summary>First name plus last initial, e.g. "Mia S." Falls back to "Customer"
    /// when the account has no name on it.</summary>
    public string AuthorDisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
