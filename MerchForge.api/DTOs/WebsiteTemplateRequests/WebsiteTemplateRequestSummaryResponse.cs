using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.WebsiteTemplateRequests;

/// <summary>One row in the SuperAdmin's "Website Requests" list.</summary>
public class WebsiteTemplateRequestSummaryResponse
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string OwnerFullName { get; set; } = string.Empty;

    public string OwnerEmail { get; set; } = string.Empty;

    public string TemplateLabel { get; set; } = string.Empty;

    public string DomainName { get; set; } = string.Empty;

    public WebsiteTemplateRequestStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? FinalWebsiteUrl { get; set; }
}
