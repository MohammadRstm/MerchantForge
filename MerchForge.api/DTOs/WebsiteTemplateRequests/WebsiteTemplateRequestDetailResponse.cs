using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.WebsiteTemplateRequests;

/// <summary>Full detail shown when a SuperAdmin opens a request.</summary>
public class WebsiteTemplateRequestDetailResponse
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string OwnerFullName { get; set; } = string.Empty;

    public string OwnerEmail { get; set; } = string.Empty;

    public Guid WebsiteTemplateId { get; set; }

    public string TemplateName { get; set; } = string.Empty;

    public string TemplateLabel { get; set; } = string.Empty;

    public string DomainName { get; set; } = string.Empty;

    public string CustomizationNotes { get; set; } = string.Empty;

    public WebsiteTemplateRequestStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? BuildStartedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string? ClosedByFullName { get; set; }

    public string? FinalWebsiteUrl { get; set; }
}
