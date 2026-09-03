namespace MerchForge.api.DTOs.Storefront;

/// <summary>
/// Everything a storefront needs to decide what to render in place of the review
/// form, in one request: whether this customer may review this product at all, and
/// their existing review if they already wrote one.
///
/// Returned only to an authenticated customer, so it is always about the caller —
/// there is no way to ask this question about somebody else.
/// </summary>
public class ProductReviewEligibilityResponse
{
    /// <summary>True when the customer has at least one non-cancelled order with this
    /// business containing this product. False means the form is not offered at all.</summary>
    public bool CanReview { get; set; }

    /// <summary>The customer's own existing review, so the form can open pre-filled
    /// for editing. Null when they have not reviewed this product yet. Carries the
    /// rating and comment rather than the public display shape, because the customer
    /// is editing their own words.</summary>
    public MyProductReviewResponse? MyReview { get; set; }
}

/// <summary>
/// A customer's own review, as returned to that customer for editing. Separate from
/// StorefrontProductReviewResponse because this one includes IsHidden — a customer
/// whose review has been hidden by the owner should see that, rather than being
/// puzzled that their review is missing from the public list.
/// </summary>
public class MyProductReviewResponse
{
    public Guid Id { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public bool IsHidden { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
