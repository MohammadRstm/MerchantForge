namespace MerchForge.api.DTOs.ImageSuggestion;

public class SuggestFromImageRequest
{
    /// <summary>One of this product's already-uploaded image urls — ownership is re-verified server-side, nothing here is trusted as-sent.</summary>
    public string ImageUrl { get; set; } = string.Empty;
}
