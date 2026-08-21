namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>One selectable template, offered to a business owner choosing a website.</summary>
public class WebsiteTemplateOptionResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string VideoPreviewUrl { get; set; } = string.Empty;
}
