using MerchForge.api.Authorization.Requirements;
using MerchForge.api.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MerchForge.api.Models;
using MerchForge.api.Services.Subscription.interfaces;

namespace MerchForge.api.Authorization.Handlers
{
    public class FeatureHandler : AuthorizationHandler<FeatureRequirement>
    {
        private readonly ISubscriptionService _subscriptionService;

        public FeatureHandler(
            ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            FeatureRequirement requirement)
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

            if (!Guid.TryParse(businessIdValue, out var businessId))
            {
                return;
            }

            var hasAccess = await _subscriptionService.HasFeatureAsync(
                businessId,
                requirement.FeatureKey);

            if (hasAccess)
            {
                context.Succeed(requirement);
            }
        }
    }
}
