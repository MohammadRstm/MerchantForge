using MerchForge.api.DTOs.Common;

namespace MerchForge.api.DTOs.Dashboard;

public class DashboardStatsResponse
{
    public int TotalUsers { get; set; }

    public int TotalBusinesses { get; set; }

    public int TotalProducts { get; set; }

    public int TotalProductDrafts { get; set; }

    public int PendingInvitations { get; set; }

    /// <summary>Pending or InProgress — a request that still needs the admin to act on it.</summary>
    public int PendingWebsiteTemplateRequests { get; set; }

    public int CompletedWebsiteTemplateRequests { get; set; }

    /// <summary>Distinct users with at least one active (non-revoked, non-expired) session right now.</summary>
    public int ActiveSessionCount { get; set; }

    public List<KeyCountResponse> UsersBySystemRole { get; set; } = new();

    public List<KeyCountResponse> BusinessUsersByRole { get; set; } = new();

    public List<KeyCountResponse> BusinessesByDomain { get; set; } = new();

    public List<KeyCountResponse> SubscriptionsByStatus { get; set; } = new();

    public List<DashboardBusinessResponse> RecentBusinesses { get; set; } = new();

    public List<TimeSeriesPointResponse> BusinessesOverTime { get; set; } = new();

    public List<TimeSeriesPointResponse> ProductsOverTime { get; set; } = new();
}
