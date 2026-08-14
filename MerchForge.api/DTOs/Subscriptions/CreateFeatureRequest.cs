namespace MerchForge.api.DTOs.Subscriptions
{
    public class CreateFeatureRequest
    {
        public string Key { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string? Description { get; set; }
    }
}
