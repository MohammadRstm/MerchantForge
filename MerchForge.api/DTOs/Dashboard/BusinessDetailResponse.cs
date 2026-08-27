using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.WebsiteTemplateRequests;

namespace MerchForge.api.DTOs.Dashboard;

/// <summary>
/// Everything a SuperAdmin sees when inspecting one business — profile, owner,
/// members, product/draft stats, website/template status and request history,
/// subscription, and feature-credit balances. Composed entirely from data already
/// exposed piecemeal elsewhere; see DashboardService.GetBusinessDetailAsync.
/// </summary>
public class BusinessDetailResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;

    public string? ContactEmail { get; set; }

    public string? ContactPhone { get; set; }

    public Guid? BusinessDomainId { get; set; }

    public string? DomainName { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid OwnerUserId { get; set; }

    public string OwnerFullName { get; set; } = string.Empty;

    public string OwnerEmail { get; set; } = string.Empty;

    public List<BusinessMemberResponse> Members { get; set; } = new();

    public int ProductCount { get; set; }

    public decimal? AverageProductPrice { get; set; }

    public decimal? MinProductPrice { get; set; }

    public decimal? MaxProductPrice { get; set; }

    public List<KeyCountResponse> ProductsByCategory { get; set; } = new();

    public int ProductDraftCount { get; set; }

    public List<KeyCountResponse> ProductDraftsByStatus { get; set; } = new();

    public string? WebsiteUrl { get; set; }

    public Guid? WebsiteTemplateId { get; set; }

    public string? WebsiteTemplateName { get; set; }

    public string? WebsiteTemplateLabel { get; set; }

    public DateTime? WebsiteTemplateChosenAt { get; set; }

    public List<WebsiteTemplateRequestResponse> WebsiteTemplateRequests { get; set; } = new();

    public BusinessSubscriptionResponse? Subscription { get; set; }

    public List<BusinessFeatureCreditResponse> FeatureCredits { get; set; } = new();
}
