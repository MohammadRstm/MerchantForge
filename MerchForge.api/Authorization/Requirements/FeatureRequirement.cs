namespace MerchForge.api.Authorization.Requirements
{
    public class FeatureRequirement
    {
        public string FeatureKey { get; }

        public FeatureRequirement(string featureKey)
        {
            FeatureKey = featureKey;
        }
    }
}
