using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;

namespace MerchForge.api.Services.Dashboard.interfaces
{
    public interface IDashboardService
    {
        Task<DashboardStatsResponse> GetPlatformStatsAsync(CancellationToken cancellationToken = default);

        Task<PagedResult<DashboardUserResponse>> GetUsersAsync(
            UsersQueryRequest query,
            CancellationToken cancellationToken = default);

        Task<RevokeUserSessionsResponse> RevokeUserSessionsAsync(
            Guid targetUserId,
            Guid actingUserId,
            CancellationToken cancellationToken = default);
    }
}
