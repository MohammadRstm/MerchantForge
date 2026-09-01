namespace MerchForge.api.DTOs.Dashboard;

/// <summary>One business currently live on a given template — shown in the template detail modal.</summary>
public class WebsiteTemplateBusinessResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime? ChosenAt { get; set; }
}
