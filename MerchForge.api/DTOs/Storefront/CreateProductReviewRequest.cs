namespace MerchForge.api.DTOs.Storefront;

/// <summary>
/// A customer submitting or editing their review of a product. The product and the
/// customer both come from outside the body — the product from the route, the
/// customer from the "Customer" bearer token — so neither can be spoofed by the
/// caller.
/// </summary>
public class CreateProductReviewRequest
{
    /// <summary>1-5, required. A review with no rating is not a review.</summary>
    public int Rating { get; set; }

    /// <summary>Optional. Null or blank submits a rating on its own.</summary>
    public string? Comment { get; set; }
}
