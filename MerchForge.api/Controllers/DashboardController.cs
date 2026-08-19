using MerchForge.api.Authorization;
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

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsResponse>> GetStats(
            CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetPlatformStatsAsync(cancellationToken);

            return Ok(response);
        }
    }
}
