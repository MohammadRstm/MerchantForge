using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Exceptions.Dashboard;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Dashboard.interfaces;

namespace MerchForge.api.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private const int StatsTimeSeriesMonths = 6;

        private readonly IDashboardRepository _dashboardRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public DashboardService(
            IDashboardRepository dashboardRepository,
            IRefreshTokenRepository refreshTokenRepository)
        {
            _dashboardRepository = dashboardRepository;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<DashboardStatsResponse> GetPlatformStatsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            var seriesStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddMonths(-(StatsTimeSeriesMonths - 1));

            var totalUsers = await _dashboardRepository.CountUsersAsync(cancellationToken);
            var totalBusinesses = await _dashboardRepository.CountBusinessesAsync(cancellationToken);
            var totalProducts = await _dashboardRepository.CountProductsAsync(cancellationToken);
            var totalProductDrafts = await _dashboardRepository.CountProductDraftsAsync(cancellationToken);
            var pendingInvitations = await _dashboardRepository.CountPendingInvitationsAsync(cancellationToken);

            var usersBySystemRole = await _dashboardRepository.GetUserCountsBySystemRoleAsync(cancellationToken);
            var businessUsersByRole = await _dashboardRepository.GetBusinessUserCountsByRoleAsync(cancellationToken);

            var businessDates = await _dashboardRepository.GetBusinessCreationDatesSinceAsync(seriesStart, cancellationToken);
            var productDates = await _dashboardRepository.GetProductCreationDatesSinceAsync(seriesStart, cancellationToken);

            return new DashboardStatsResponse
            {
                TotalUsers = totalUsers,
                TotalBusinesses = totalBusinesses,
                TotalProducts = totalProducts,
                TotalProductDrafts = totalProductDrafts,
                PendingInvitations = pendingInvitations,

                UsersBySystemRole = usersBySystemRole,
                BusinessUsersByRole = businessUsersByRole,

                BusinessesOverTime = BuildMonthlySeries(businessDates, seriesStart, now),
                ProductsOverTime = BuildMonthlySeries(productDates, seriesStart, now),
            };
        }

        public async Task<PagedResult<DashboardUserResponse>> GetUsersAsync(
            UsersQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _dashboardRepository.GetUsersAsync(query, cancellationToken);

            return new PagedResult<DashboardUserResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<RevokeUserSessionsResponse> RevokeUserSessionsAsync(
            Guid targetUserId,
            Guid actingUserId,
            CancellationToken cancellationToken = default)
        {
            if (targetUserId == actingUserId)
            {
                throw new CannotRevokeOwnSessionException();
            }

            var userExists = await _dashboardRepository.UserExistsAsync(targetUserId, cancellationToken);

            if (!userExists)
            {
                throw new UserNotFoundException();
            }

            var revokedCount = await _refreshTokenRepository.RevokeAllForUserAsync(targetUserId, cancellationToken);

            return new RevokeUserSessionsResponse
            {
                RevokedSessionsCount = revokedCount
            };
        }

        public async Task<PagedResult<DashboardBusinessResponse>> GetBusinessesAsync(
            BusinessesQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _dashboardRepository.GetBusinessesAsync(query, cancellationToken);

            return new PagedResult<DashboardBusinessResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        private static List<TimeSeriesPointResponse> BuildMonthlySeries(
            List<DateTime> dates,
            DateTime seriesStart,
            DateTime until)
        {
            var series = new List<TimeSeriesPointResponse>();

            var cursor = seriesStart;
            var end = new DateTime(until.Year, until.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            while (cursor <= end)
            {
                var nextMonth = cursor.AddMonths(1);

                var count = dates.Count(d => d >= cursor && d < nextMonth);

                series.Add(new TimeSeriesPointResponse
                {
                    Period = cursor.ToString("yyyy-MM"),
                    Count = count,
                });

                cursor = nextMonth;
            }

            return series;
        }
    }
}
