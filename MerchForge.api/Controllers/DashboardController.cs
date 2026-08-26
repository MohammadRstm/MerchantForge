using System.Security.Claims;
using FluentValidation;
using MerchForge.api.Authorization;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
using MerchForge.api.DTOs.WebsiteTemplateRequests;
using MerchForge.api.Services.Dashboard.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MerchForge.api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = AuthorizationPolicies.SystemSuperAdmin)]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly IValidator<UsersQueryRequest> _usersQueryValidator;
        private readonly IValidator<BusinessesQueryRequest> _businessesQueryValidator;
        private readonly IValidator<CreateWebsiteTemplateRequest> _createWebsiteTemplateValidator;
        private readonly IValidator<WebsiteTemplateRequestsQueryRequest> _websiteTemplateRequestsQueryValidator;
        private readonly IValidator<CloseWebsiteTemplateRequestRequest> _closeWebsiteTemplateRequestValidator;

        public DashboardController(
            IDashboardService dashboardService,
            IValidator<UsersQueryRequest> usersQueryValidator,
            IValidator<BusinessesQueryRequest> businessesQueryValidator,
            IValidator<CreateWebsiteTemplateRequest> createWebsiteTemplateValidator,
            IValidator<WebsiteTemplateRequestsQueryRequest> websiteTemplateRequestsQueryValidator,
            IValidator<CloseWebsiteTemplateRequestRequest> closeWebsiteTemplateRequestValidator)
        {
            _dashboardService = dashboardService;
            _usersQueryValidator = usersQueryValidator;
            _businessesQueryValidator = businessesQueryValidator;
            _createWebsiteTemplateValidator = createWebsiteTemplateValidator;
            _websiteTemplateRequestsQueryValidator = websiteTemplateRequestsQueryValidator;
            _closeWebsiteTemplateRequestValidator = closeWebsiteTemplateRequestValidator;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsResponse>> GetStats(
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetPlatformStatsAsync(cancellationToken);

            return Ok(response);
        }

        [HttpGet("users")]
        public async Task<ActionResult<PagedResult<DashboardUserResponse>>> GetUsers(
            [FromQuery] UsersQueryRequest query,
            CancellationToken cancellationToken)
        {
            await _usersQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

            var response = await _dashboardService.GetUsersAsync(query, cancellationToken);

            return Ok(response);
        }

        [HttpPost("users/{userId:guid}/revoke-sessions")]
        public async Task<ActionResult<RevokeUserSessionsResponse>> RevokeUserSessions(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var actingUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(actingUserId, out var parsedActingUserId))
            {
                return Unauthorized();
            }

            var response = await _dashboardService.RevokeUserSessionsAsync(
                userId,
                parsedActingUserId,
                cancellationToken);

            return Ok(response);
        }

        [HttpGet("businesses")]
        public async Task<ActionResult<PagedResult<DashboardBusinessResponse>>> GetBusinesses(
            [FromQuery] BusinessesQueryRequest query,
            CancellationToken cancellationToken)
        {
            await _businessesQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

            var response = await _dashboardService.GetBusinessesAsync(query, cancellationToken);

            return Ok(response);
        }

        // ---- website templates ----

        [HttpGet("website-templates")]
        public async Task<ActionResult<List<WebsiteTemplateResponse>>> GetWebsiteTemplates(
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetWebsiteTemplatesAsync(cancellationToken);

            return Ok(response);
        }

        [HttpPost("website-templates")]
        public async Task<ActionResult<WebsiteTemplateResponse>> CreateWebsiteTemplate(
            [FromBody] CreateWebsiteTemplateRequest request,
            CancellationToken cancellationToken)
        {
            await _createWebsiteTemplateValidator.ValidateAndThrowAsync(request, cancellationToken);

            var response = await _dashboardService.CreateWebsiteTemplateAsync(request, cancellationToken);

            return Ok(response);
        }

        // ---- website template requests ----

        [HttpGet("website-template-requests")]
        public async Task<ActionResult<PagedResult<WebsiteTemplateRequestSummaryResponse>>> GetWebsiteTemplateRequests(
            [FromQuery] WebsiteTemplateRequestsQueryRequest query,
            CancellationToken cancellationToken)
        {
            await _websiteTemplateRequestsQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

            var response = await _dashboardService.GetWebsiteTemplateRequestsAsync(query, cancellationToken);

            return Ok(response);
        }

        [HttpGet("website-template-requests/{websiteTemplateRequestId:guid}")]
        public async Task<ActionResult<WebsiteTemplateRequestDetailResponse>> GetWebsiteTemplateRequest(
            Guid websiteTemplateRequestId,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetWebsiteTemplateRequestAsync(websiteTemplateRequestId, cancellationToken);

            return Ok(response);
        }

        [HttpPost("website-template-requests/{websiteTemplateRequestId:guid}/start-build")]
        public async Task<ActionResult<WebsiteTemplateRequestDetailResponse>> StartWebsiteTemplateRequestBuild(
            Guid websiteTemplateRequestId,
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.StartWebsiteTemplateRequestBuildAsync(
                websiteTemplateRequestId, cancellationToken);

            return Ok(response);
        }

        [HttpPost("website-template-requests/{websiteTemplateRequestId:guid}/close")]
        public async Task<ActionResult<WebsiteTemplateRequestDetailResponse>> CloseWebsiteTemplateRequest(
            Guid websiteTemplateRequestId,
            [FromBody] CloseWebsiteTemplateRequestRequest request,
            CancellationToken cancellationToken)
        {
            await _closeWebsiteTemplateRequestValidator.ValidateAndThrowAsync(request, cancellationToken);

            var closedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(closedByUserId, out var parsedClosedByUserId))
            {
                return Unauthorized();
            }

            var response = await _dashboardService.CloseWebsiteTemplateRequestAsync(
                websiteTemplateRequestId,
                parsedClosedByUserId,
                request,
                cancellationToken);

            return Ok(response);
        }
    }
}
