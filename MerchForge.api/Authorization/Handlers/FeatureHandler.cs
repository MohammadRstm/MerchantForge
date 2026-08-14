using MerchForge.api.Authorization.Requirements;
using MerchForge.api.Data;
using Microsoft.AspNetCore.Authorization;

namespace MerchForge.api.Authorization.Handlers
{
    public class FeatureHandler
    {
        private readonly MerchForgeDbContext _db;

        public FeatureHandler(MerchForgeDbContext db)
        {
            _db = db;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            FeatureRequirement requirement)
        {
            // Determine the business being accessed
            // Determine its active subscription
            // Determine its plan
            // Determine whether the plan contains the feature

            // If it does:
            context.Succeed(requirement);
        }
    }
}
