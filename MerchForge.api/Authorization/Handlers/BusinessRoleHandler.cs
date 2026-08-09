using MerchForge.api.Data;
using MerchForge.api.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace MerchForge.api.Authorization.Handlers
{
    public class BusinessRoleHandler : AuthorizationHandler<BusinessRoleRequirements>
    {
        private readonly MerchForgeDbContext _db;
        public BusinessRoleHandler(MerchForgeDbContext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(
             AuthorizationHandlerContext context,
             BusinessRoleRequirements requirement)
        {
            var userId = context.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return;
            }

            if (context.Resource is not HttpContext httpContext)
            {
                return;
            }

            var businessIdValue =
                httpContext.Request.RouteValues["businessId"]?.ToString();

            if (!Guid.TryParse(
                    businessIdValue,
                    out var businessId))
            {
                return;
            }

            var userRole = await _db.BusinessUsers
                .Where(bu =>
                    bu.UserId == parsedUserId &&
                    bu.BusinessId == businessId)
                .Select(bu => bu.Role)
                .FirstOrDefaultAsync();

            if (requirement.AllowedRoles.Contains(userRole))
            {
                context.Succeed(requirement);
            }
        }

    }
}
