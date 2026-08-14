namespace MerchForge.api.Services.Subscription.interfaces
{
    using MerchForge.api.Models;
    public interface ISubscriptionService
    {
        Task<bool> HasFeatureAsync(
            Guid businessId,
            string featureKey);
    }
}
