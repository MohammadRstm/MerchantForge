using MerchForge.api.DTOs.Dashboard;

namespace MerchForge.api.Services.Dashboard.interfaces
{
    public interface IDashboardService
    {
        Task<DashboardStatsResponse> GetPlatformStatsAsync(CancellationToken cancellationToken = default);
    }
}
