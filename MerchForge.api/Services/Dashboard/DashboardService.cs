using Hangfire;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.DTOs.WebsiteTemplateRequests;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.Dashboard;
using MerchForge.api.Exceptions.WebsiteTemplateRequests;
using MerchForge.api.Jobs.Email;
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
        private readonly IWebsiteTemplateRequestRepository _websiteTemplateRequestRepository;
        private readonly IWebsiteTemplateVideoService _websiteTemplateVideoService;
        private readonly IDomainService _domainService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public DashboardService(
            IDashboardRepository dashboardRepository,
            IRefreshTokenRepository refreshTokenRepository,
            IWebsiteTemplateRequestRepository websiteTemplateRequestRepository,
            IWebsiteTemplateVideoService websiteTemplateVideoService,
            IDomainService domainService,
            IBackgroundJobClient backgroundJobClient)
        {
            _dashboardRepository = dashboardRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _websiteTemplateRequestRepository = websiteTemplateRequestRepository;
            _websiteTemplateVideoService = websiteTemplateVideoService;
            _domainService = domainService;
            _backgroundJobClient = backgroundJobClient;
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
                PreviewWebsiteUrl = string.IsNullOrWhiteSpace(request.PreviewWebsiteUrl) ? null : request.PreviewWebsiteUrl.Trim(),
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
                PreviewWebsiteUrl = template.PreviewWebsiteUrl,
                IsActive = template.IsActive,
                DisplayOrder = template.DisplayOrder,
                BusinessesUsingIt = 0,
                CreatedAt = template.CreatedAt,
            };
        }

        public async Task<string> UploadWebsiteTemplateVideoAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            return await _websiteTemplateVideoService.SaveAsync(file, cancellationToken);
        }

        public async Task<WebsiteTemplateDetailResponse> GetWebsiteTemplateDetailAsync(
            Guid websiteTemplateId,
            CancellationToken cancellationToken = default)
        {
            return await _dashboardRepository.GetWebsiteTemplateDetailAsync(websiteTemplateId, cancellationToken)
                ?? throw new WebsiteTemplateNotFoundException();
        }

        public async Task<WebsiteTemplateResponse> UpdateWebsiteTemplateAsync(
            Guid websiteTemplateId,
            UpdateWebsiteTemplateRequest request,
            CancellationToken cancellationToken = default)
        {
            var template = await _dashboardRepository.GetTrackedWebsiteTemplateAsync(websiteTemplateId, cancellationToken)
                ?? throw new WebsiteTemplateNotFoundException();

            template.Label = request.Label;
            template.VideoPreviewUrl = request.VideoPreviewUrl;
            template.PreviewWebsiteUrl = string.IsNullOrWhiteSpace(request.PreviewWebsiteUrl) ? null : request.PreviewWebsiteUrl.Trim();
            template.DisplayOrder = request.DisplayOrder;
            template.UpdatedAt = DateTime.UtcNow;

            await _dashboardRepository.SaveChangesAsync(cancellationToken);

            return await MapToResponseAsync(websiteTemplateId, cancellationToken);
        }

        public async Task<WebsiteTemplateResponse> DeactivateWebsiteTemplateAsync(
            Guid websiteTemplateId,
            CancellationToken cancellationToken = default)
        {
            var template = await _dashboardRepository.GetTrackedWebsiteTemplateAsync(websiteTemplateId, cancellationToken)
                ?? throw new WebsiteTemplateNotFoundException();

            // A soft delete: IsActive already exists precisely so retiring a template
            // never removes it out from under a business that already lives on it.
            template.IsActive = false;
            template.UpdatedAt = DateTime.UtcNow;

            await _dashboardRepository.SaveChangesAsync(cancellationToken);

            return await MapToResponseAsync(websiteTemplateId, cancellationToken);
        }

        private async Task<WebsiteTemplateResponse> MapToResponseAsync(
            Guid websiteTemplateId,
            CancellationToken cancellationToken)
        {
            var detail = await _dashboardRepository.GetWebsiteTemplateDetailAsync(websiteTemplateId, cancellationToken)
                ?? throw new WebsiteTemplateNotFoundException();

            return new WebsiteTemplateResponse
            {
                Id = detail.Id,
                BusinessDomainId = detail.BusinessDomainId,
                DomainName = detail.DomainName,
                Name = detail.Name,
                Label = detail.Label,
                VideoPreviewUrl = detail.VideoPreviewUrl,
                PreviewWebsiteUrl = detail.PreviewWebsiteUrl,
                IsActive = detail.IsActive,
                DisplayOrder = detail.DisplayOrder,
                BusinessesUsingIt = detail.Businesses.Count,
                CreatedAt = detail.CreatedAt,
            };
        }

        // ---- website template requests ----

        public async Task<PagedResult<WebsiteTemplateRequestSummaryResponse>> GetWebsiteTemplateRequestsAsync(
            WebsiteTemplateRequestsQueryRequest query,
            CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _websiteTemplateRequestRepository.GetPagedAsync(query, cancellationToken);

            return new PagedResult<WebsiteTemplateRequestSummaryResponse>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
            };
        }

        public async Task<WebsiteTemplateRequestDetailResponse> GetWebsiteTemplateRequestAsync(
            Guid websiteTemplateRequestId,
            CancellationToken cancellationToken = default)
        {
            return await _websiteTemplateRequestRepository.GetDetailByIdAsync(websiteTemplateRequestId, cancellationToken)
                ?? throw new WebsiteTemplateRequestNotFoundException();
        }

        public async Task<WebsiteTemplateRequestDetailResponse> StartWebsiteTemplateRequestBuildAsync(
            Guid websiteTemplateRequestId,
            CancellationToken cancellationToken = default)
        {
            var request = await _websiteTemplateRequestRepository.GetTrackedByIdAsync(websiteTemplateRequestId, cancellationToken)
                ?? throw new WebsiteTemplateRequestNotFoundException();

            if (request.Status != WebsiteTemplateRequestStatus.Pending)
            {
                throw new WebsiteTemplateRequestInvalidStatusTransitionException();
            }

            request.Status = WebsiteTemplateRequestStatus.InProgress;
            request.BuildStartedAt = DateTime.UtcNow;

            await _websiteTemplateRequestRepository.SaveChangesAsync(cancellationToken);

            _backgroundJobClient.Enqueue<NotifyOwnerOfWebsiteBuildStartedJob>(
                job => job.ExecuteAsync(websiteTemplateRequestId));

            return await GetWebsiteTemplateRequestAsync(websiteTemplateRequestId, cancellationToken);
        }

        public async Task<WebsiteTemplateRequestDetailResponse> CloseWebsiteTemplateRequestAsync(
            Guid websiteTemplateRequestId,
            Guid closedByUserId,
            CloseWebsiteTemplateRequestRequest request,
            CancellationToken cancellationToken = default)
        {
            var websiteTemplateRequest = await _websiteTemplateRequestRepository.GetTrackedByIdAsync(
                websiteTemplateRequestId, cancellationToken)
                ?? throw new WebsiteTemplateRequestNotFoundException();

            if (websiteTemplateRequest.Status == WebsiteTemplateRequestStatus.Closed)
            {
                throw new WebsiteTemplateRequestInvalidStatusTransitionException();
            }

            websiteTemplateRequest.Status = WebsiteTemplateRequestStatus.Closed;
            websiteTemplateRequest.ClosedAt = DateTime.UtcNow;
            websiteTemplateRequest.ClosedByUserId = closedByUserId;
            websiteTemplateRequest.FinalWebsiteUrl = request.FinalWebsiteUrl.Trim();

            await _websiteTemplateRequestRepository.SaveChangesAsync(cancellationToken);

            // The template this business is now actually running — set here, on
            // close, rather than when the request was merely submitted, and free to
            // overwrite an earlier value from a prior closed request.
            await _websiteTemplateRequestRepository.SetBusinessActiveWebsiteTemplateAsync(
                websiteTemplateRequest.BusinessId,
                websiteTemplateRequest.WebsiteTemplateId,
                cancellationToken);

            return await GetWebsiteTemplateRequestAsync(websiteTemplateRequestId, cancellationToken);
        }
    }
}
