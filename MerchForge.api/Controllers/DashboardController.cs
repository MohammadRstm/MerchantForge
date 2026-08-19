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

        public DashboardController(
            IDashboardService dashboardService,
            IValidator<UsersQueryRequest> usersQueryValidator)
        {
            _dashboardService = dashboardService;
            _usersQueryValidator = usersQueryValidator;
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
    }
}
