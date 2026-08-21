namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>The template a business has already chosen.</summary>
public class ChosenWebsiteTemplateResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string VideoPreviewUrl { get; set; } = string.Empty;

    public DateTime ChosenAt { get; set; }
}
