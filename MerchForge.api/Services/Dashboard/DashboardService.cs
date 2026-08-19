using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Dashboard.interfaces;

namespace MerchForge.api.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private const int StatsTimeSeriesMonths = 6;

        private readonly IDashboardRepository _dashboardRepository;

        public DashboardService(IDashboardRepository dashboardRepository)
        {
            _dashboardRepository = dashboardRepository;
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
