namespace MerchForge.api.DTOs.Dashboard;

/// <summary>Everything WebsiteTemplateResponse has, plus the businesses currently using it — shown when a SuperAdmin opens a template.</summary>
public class WebsiteTemplateDetailResponse
{
    public Guid Id { get; set; }

    public Guid BusinessDomainId { get; set; }

    public string DomainName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string PreviewImageUrl { get; set; } = string.Empty;

    public string? PreviewWebsiteUrl { get; set; }

    public bool IsActive { get; set; }

    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int RequestCount { get; set; }

    public int ActiveCustomizableComponentCount { get; set; }

    public List<WebsiteTemplateBusinessResponse> Businesses { get; set; } = [];
}
