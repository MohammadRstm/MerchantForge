using MerchForge.api.Models;

namespace MerchForge.api.Repositories.Interfaces
{
    public interface ISubscriptionRepository
    {
        Task<Subscription?> GetSubscriptionWithPlanFeaturesAsync(Guid businessId);

        Task<Subscription?> GetLatestSubscriptionWithPlanFeaturesAsync(Guid businessId, CancellationToken cancellationToken = default);
    }
}
