namespace MerchForge.api.DTOs.Storefront;

public class StorefrontProductImageResponse
{
    public Guid Id { get; set; }

    public string Url { get; set; } = string.Empty;

    public bool IsMain { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public string? AltText { get; set; }

    public int DisplayOrder { get; set; }
}
