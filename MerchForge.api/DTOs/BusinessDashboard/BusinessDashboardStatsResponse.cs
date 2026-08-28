using MerchForge.api.DTOs.Common;

namespace MerchForge.api.DTOs.BusinessDashboard;

public class BusinessDashboardStatsResponse
{
    public Guid BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    /// <summary>The deployed URL of this business's live website. Null until a website template request for it has been closed — the dashboard's "View website" button gates on this.</summary>
    public string? WebsiteUrl { get; set; }

    public int MemberCount { get; set; }

    public int ProductCount { get; set; }

    public int ProductDraftCount { get; set; }

    public decimal? AverageProductPrice { get; set; }

    public decimal? MinProductPrice { get; set; }

    public decimal? MaxProductPrice { get; set; }

    public int OutOfStockProductCount { get; set; }

    public int OrderCount { get; set; }

    public int PendingOrderCount { get; set; }

    public List<BusinessProductResponse> RecentProducts { get; set; } = new();

    public List<KeyCountResponse> ProductsByCategory { get; set; } = new();

    public List<KeyCountResponse> ProductDraftsByStatus { get; set; } = new();

    public List<KeyCountResponse> MembersByRole { get; set; } = new();

    public List<TimeSeriesPointResponse> ProductsOverTime { get; set; } = new();
}
