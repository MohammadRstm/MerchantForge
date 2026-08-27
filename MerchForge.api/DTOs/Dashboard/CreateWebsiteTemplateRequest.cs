namespace MerchForge.api.DTOs.Dashboard;

public class CreateWebsiteTemplateRequest
{
    public Guid BusinessDomainId { get; set; }

    /// <summary>Technical identifier, e.g. "fashion-template-02". Must be globally unique.</summary>
    public string Name { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string PreviewImageUrl { get; set; } = string.Empty;

    public string? PreviewWebsiteUrl { get; set; }

    public int DisplayOrder { get; set; }
}
