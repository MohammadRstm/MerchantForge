using System.Security.Claims;
using FluentValidation;
using MerchForge.api.Authorization;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Dashboard;
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

        public DashboardController(
            IDashboardService dashboardService,
            IValidator<UsersQueryRequest> usersQueryValidator,
            IValidator<BusinessesQueryRequest> businessesQueryValidator,
            IValidator<CreateWebsiteTemplateRequest> createWebsiteTemplateValidator)
        {
            _dashboardService = dashboardService;
            _usersQueryValidator = usersQueryValidator;
            _businessesQueryValidator = businessesQueryValidator;
            _createWebsiteTemplateValidator = createWebsiteTemplateValidator;
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
    }
}
