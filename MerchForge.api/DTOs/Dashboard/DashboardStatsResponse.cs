using MerchForge.api.DTOs.Common;

namespace MerchForge.api.DTOs.Dashboard;

public class DashboardStatsResponse
{
    public int TotalUsers { get; set; }

    public int TotalBusinesses { get; set; }

    public int TotalProducts { get; set; }

    public int TotalProductDrafts { get; set; }

    public int PendingInvitations { get; set; }

    public List<RoleCountResponse> UsersBySystemRole { get; set; } = new();

    public List<RoleCountResponse> BusinessUsersByRole { get; set; } = new();

    public List<TimeSeriesPointResponse> BusinessesOverTime { get; set; } = new();

    public List<TimeSeriesPointResponse> ProductsOverTime { get; set; } = new();
}
