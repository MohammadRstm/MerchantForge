using MerchForge.api.DTOs.Subscriptions;

namespace MerchForge.api.Services.Subscription.interfaces;

/// <summary>SuperAdmin management of the subscription plan catalogue.</summary>
public interface ISubscriptionPlanService
{
    Task<List<SubscriptionPlanResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Active plans only, for the public-facing landing/billing pages.</summary>
    Task<List<SubscriptionPlanResponse>> GetPublicAsync(CancellationToken cancellationToken = default);

    Task<SubscriptionPlanDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<FeatureResponse>> GetFeaturesAsync(CancellationToken cancellationToken = default);

    Task<SubscriptionPlanResponse> CreateAsync(
        CreateSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<SubscriptionPlanResponse> UpdateAsync(
        Guid id,
        UpdateSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<SubscriptionPlanResponse> SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default);
}
