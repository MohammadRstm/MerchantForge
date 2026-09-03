namespace MerchForge.api.DTOs.Storefront;

/// <summary>
/// Aggregate rating figures for one product, computed over its visible reviews only —
/// a hidden review counts for nothing, in the average or the breakdown.
/// </summary>
public class ProductReviewSummaryResponse
{
    /// <summary>Null when the product has no visible reviews, so a storefront can tell
    /// "no reviews yet" apart from a genuine average that happens to be low.</summary>
    public decimal? AverageRating { get; set; }

    public int ReviewCount { get; set; }

    /// <summary>How many reviews gave each star value. Always has all five entries,
    /// keyed 1-5, with zeros where nobody gave that rating — so a storefront can
    /// render the full histogram without filling gaps itself.</summary>
    public Dictionary<int, int> RatingBreakdown { get; set; } = [];
}
