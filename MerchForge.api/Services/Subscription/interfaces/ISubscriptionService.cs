namespace MerchForge.api.Services.Subscription.interfaces
{
    using MerchForge.api.Models;
    public interface ISubscriptionService
    {
        /// <summary>
        /// Whether this business can use the feature at all, by any route: bundled
        /// into its plan, or bought independently as credits. What the authorization
        /// policy pipeline checks before letting a request through.
        /// </summary>
        Task<bool> HasFeatureAsync(
            Guid businessId,
            string featureKey);

        /// <summary>
        /// Plan membership only, ignoring any independent credit purchase. Used to
        /// decide whether a feature's usage is unlimited (bundled in the plan) or
        /// must be metered against a credit balance.
        /// </summary>
        Task<bool> HasPlanFeatureAsync(
            Guid businessId,
            string featureKey);
    }
}
