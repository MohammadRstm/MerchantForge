using Microsoft.AspNetCore.Authorization;

namespace MerchForge.api.Authorization.Requirements
{
    public class FeatureRequirement : IAuthorizationRequirement
    {
        public string FeatureKey { get; }

        public FeatureRequirement(string featureKey)
        {
            FeatureKey = featureKey;
        }
    }
}
