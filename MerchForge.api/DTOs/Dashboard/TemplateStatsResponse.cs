namespace MerchForge.api.DTOs.Dashboard;

public class TemplateStatsResponse
{
    public int TotalTemplates { get; set; }

    public int ActiveTemplates { get; set; }

    public int InactiveTemplates { get; set; }

    /// <summary>Distinct businesses with a WebsiteTemplateId set - not a sum across templates, since that's exactly what BusinessesUsingTemplates already is.</summary>
    public int BusinessesUsingTemplates { get; set; }

    public string? MostUsedTemplateName { get; set; }

    public int MostUsedTemplateBusinessCount { get; set; }

    /// <summary>Pending + InProgress, same definition as the platform-wide stats endpoint's PendingWebsiteTemplateRequests.</summary>
    public int PendingTemplateRequests { get; set; }
}
