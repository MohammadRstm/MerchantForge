using MerchForge.api.Data;
using MerchForge.api.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MerchForge.api.Repositories.Interfaces;


namespace MerchForge.api.Authorization.Handlers
{
    public class BusinessRoleHandler : AuthorizationHandler<BusinessRoleRequirements>
    {
        private readonly MerchForgeDbContext _db;
        private readonly IUserRepository _userRepository;
        public BusinessRoleHandler(MerchForgeDbContext db, IUserRepository userRepository)
        {
            _db = db;
            _userRepository = userRepository;
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

            var businessUser = await _db.BusinessUsers
                .FirstOrDefaultAsync(
                    bu =>
                        bu.UserId == parsedUserId &&
                        bu.BusinessId == businessId);

            if (businessUser is null)
            {
                return;
            }

            var userBusinessRole = await _userRepository.GetBusinessRoleById(businessUser.RoleId);

            if (requirement.AllowedRoles.Contains(userBusinessRole))
            {
                context.Succeed(requirement);
            }
        }

    }
}
