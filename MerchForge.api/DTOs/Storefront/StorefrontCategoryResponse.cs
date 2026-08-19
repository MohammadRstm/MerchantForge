namespace MerchForge.api.DTOs.Storefront;

/// <summary>
/// A category available to this storefront, with the number of products the business
/// actually has in it.
///
/// ProductCount is included so a storefront can decide for itself whether to hide
/// empty categories — that is a presentation choice, and the SDK supplies the data
/// rather than making it.
/// </summary>
public class StorefrontCategoryResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    /// <summary>Suggested navigation ordering. Honouring it is a UI decision.</summary>
    public int DisplayOrder { get; set; }

    public int ProductCount { get; set; }
}
