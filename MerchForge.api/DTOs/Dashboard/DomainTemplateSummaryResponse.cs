namespace MerchForge.api.DTOs.Dashboard;

/// <summary>How many templates each domain has, and how many businesses in total are using them - the domain <-> template relationship at a glance.</summary>
public class DomainTemplateSummaryResponse
{
    public Guid BusinessDomainId { get; set; }

    public string DomainName { get; set; } = string.Empty;

    public int TemplateCount { get; set; }

    public int BusinessCount { get; set; }
}
