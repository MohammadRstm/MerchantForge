using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.Exceptions.Dashboard;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Common;
using MerchForge.api.Services.Dashboard.interfaces;
using MerchForge.api.Services.Onboarding.interfaces;

namespace MerchForge.api.Services.Dashboard
{
    public class DashboardService : IDashboardService
    {
        private const int StatsTimeSeriesMonths = 6;

        private readonly IDashboardRepository _dashboardRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IDomainService _domainService;

        public DashboardService(
            IDashboardRepository dashboardRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IDomainService domainService)
        {
            _dashboardRepository = dashboardRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _domainService = domainService;
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

                BusinessesOverTime = TimeSeriesBuilder.BuildMonthlySeries(businessDates, seriesStart, now),
                ProductsOverTime = TimeSeriesBuilder.BuildMonthlySeries(productDates, seriesStart, now),
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

        // ---- website templates ----

        public async Task<List<WebsiteTemplateResponse>> GetWebsiteTemplatesAsync(CancellationToken cancellationToken = default)
        {
            return await _dashboardRepository.GetWebsiteTemplatesAsync(cancellationToken);
        }

        public async Task<WebsiteTemplateResponse> CreateWebsiteTemplateAsync(
            CreateWebsiteTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            await _domainService.EnsureDomainExistsAsync(request.BusinessDomainId, cancellationToken);

            if (await _dashboardRepository.WebsiteTemplateNameExistsAsync(request.Name, cancellationToken))
            {
                throw new WebsiteTemplateNameAlreadyExistsException();
            }

            var template = new WebsiteTemplate
            {
                Id = Guid.NewGuid(),
                BusinessDomainId = request.BusinessDomainId,
                Name = request.Name,
                Label = request.Label,
                VideoPreviewUrl = request.VideoPreviewUrl,
                DisplayOrder = request.DisplayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            await _dashboardRepository.CreateWebsiteTemplateAsync(template, cancellationToken);

            var domains = await _domainService.GetDomainsAsync(cancellationToken);
            var domainName = domains.FirstOrDefault(d => d.Id == template.BusinessDomainId)?.Name ?? string.Empty;

            return new WebsiteTemplateResponse
            {
                Id = template.Id,
                BusinessDomainId = template.BusinessDomainId,
                DomainName = domainName,
                Name = template.Name,
                Label = template.Label,
                VideoPreviewUrl = template.VideoPreviewUrl,
                IsActive = template.IsActive,
                DisplayOrder = template.DisplayOrder,
                BusinessesUsingIt = 0,
                CreatedAt = template.CreatedAt,
            };
        }
    }
}
