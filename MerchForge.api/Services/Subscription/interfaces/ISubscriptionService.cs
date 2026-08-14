namespace MerchForge.api.Services.Subscription.interfaces
{
    using MerchForge.api.Models;
    public interface ISubscriptionService
    {
        Task<Subscription> HasFeatureAsync(Guid businessId);

    }
}
