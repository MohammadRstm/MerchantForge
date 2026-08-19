using MerchForge.api.DTOs.Common;

namespace MerchForge.api.DTOs.BusinessDashboard;

public class BusinessDashboardStatsResponse
{
    public Guid BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public int MemberCount { get; set; }

    public int ProductCount { get; set; }

    public int ProductDraftCount { get; set; }

    public decimal? AverageProductPrice { get; set; }

    public decimal? MinProductPrice { get; set; }

    public decimal? MaxProductPrice { get; set; }

    public List<KeyCountResponse> ProductsByCategory { get; set; } = new();

    public List<KeyCountResponse> ProductDraftsByStatus { get; set; } = new();

    public List<KeyCountResponse> MembersByRole { get; set; } = new();

    public List<TimeSeriesPointResponse> ProductsOverTime { get; set; } = new();
}
