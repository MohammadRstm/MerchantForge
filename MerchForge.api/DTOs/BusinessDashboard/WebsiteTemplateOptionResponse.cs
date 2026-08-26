namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>One selectable template, offered to a business owner choosing a website.</summary>
public class WebsiteTemplateOptionResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string VideoPreviewUrl { get; set; } = string.Empty;

    /// <summary>Opened in a new tab by the "Preview" button. Null until a demo deployment exists.</summary>
    public string? PreviewWebsiteUrl { get; set; }
}
