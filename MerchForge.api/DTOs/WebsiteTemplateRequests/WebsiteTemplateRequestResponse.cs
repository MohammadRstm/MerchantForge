using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.WebsiteTemplateRequests;

/// <summary>One request as the owner who submitted it sees it.</summary>
public class WebsiteTemplateRequestResponse
{
    public Guid Id { get; set; }

    public Guid WebsiteTemplateId { get; set; }

    public string TemplateName { get; set; } = string.Empty;

    public string TemplateLabel { get; set; } = string.Empty;

    public string DomainName { get; set; } = string.Empty;

    public string CustomizationNotes { get; set; } = string.Empty;

    public WebsiteTemplateRequestStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? BuildStartedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string? FinalWebsiteUrl { get; set; }
}
