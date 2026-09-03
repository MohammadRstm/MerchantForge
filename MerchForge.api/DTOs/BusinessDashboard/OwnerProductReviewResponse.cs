namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// One review as the business owner sees it. Carries more than the storefront shape
/// on purpose: the owner is moderating their own store's content, so they see the
/// reviewer's real name and email and whether the review is currently hidden.
///
/// Still no CustomerId — the owner has no endpoint that takes one, so exposing it
/// would widen the surface without enabling anything.
/// </summary>
public class OwnerProductReviewResponse
{
    public Guid Id { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    /// <summary>Hidden reviews are excluded from the storefront and from the product's
    /// average, but still appear in this list so the owner can unhide them.</summary>
    public bool IsHidden { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
