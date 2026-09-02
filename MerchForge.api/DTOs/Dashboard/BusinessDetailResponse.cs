using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Subscriptions;
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

    /// <summary>Short marketing line — see Business.Tagline's own doc comment.</summary>
    public string? Tagline { get; set; }

    public string? WhatsAppNumber { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    /// <summary>Only set keys are populated — an unset social link means "hide the icon", per Business.SocialLinks's doc comment.</summary>
    public SocialLinksDto? SocialLinks { get; set; }

    public BusinessHoursDto? BusinessHours { get; set; }

    public string? PrimaryColor { get; set; }

    public Guid? BusinessDomainId { get; set; }

    public string? DomainName { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>True for a SuperAdmin-created showcase business — see Business.IsDemo's own doc comment.</summary>
    public bool IsDemo { get; set; }

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

    /// <summary>
    /// How many businesses (including this one, if active) currently subscribe to
    /// this business's plan — null when the business has no subscription. Reuses
    /// ISubscriptionPlanRepository.CountActiveSubscribersAsync, the same figure shown
    /// on the Plans page's own plan-detail view.
    /// </summary>
    public int? ActiveSubscriberCountForPlan { get; set; }

    /// <summary>
    /// Every purchasable feature, whether it's already unlimited under the current
    /// plan (IncludedInPlan), and this business's credit balance for it — reuses
    /// IFeatureCreditService.GetOverviewAsync rather than the narrower
    /// BusinessFeatureCreditResponse shape, which silently omits plan-bundled
    /// (unlimited) features entirely.
    /// </summary>
    public List<FeatureCreditOverviewResponse> FeatureCredits { get; set; } = new();
}
