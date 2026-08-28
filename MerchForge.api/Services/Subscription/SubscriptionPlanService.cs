using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Subscriptions;
using MerchForge.api.Exceptions.Subscriptions;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Subscription.interfaces;

namespace MerchForge.api.Services.Subscription;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _repository;

    public SubscriptionPlanService(ISubscriptionPlanRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SubscriptionPlanResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _repository.GetAllAsync(cancellationToken);

        return plans.Select(MapPlan).ToList();
    }

    public async Task<List<SubscriptionPlanResponse>> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _repository.GetActiveAsync(cancellationToken);

        return plans.Select(MapPlan).ToList();
    }

    public async Task<SubscriptionPlanDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _repository.GetByIdWithFeaturesAsync(id, cancellationToken)
            ?? throw new SubscriptionPlanNotFoundException();

        var subscriberCount = await _repository.CountActiveSubscribersAsync(id, cancellationToken);

        return MapDetail(plan, subscriberCount);
    }

    public async Task<List<FeatureResponse>> GetFeaturesAsync(CancellationToken cancellationToken = default)
    {
        var features = await _repository.GetAllFeaturesAsync(cancellationToken);

        return features.Select(f => new FeatureResponse
        {
            Id = f.Id,
            Key = f.Key,
            Name = f.Name,
            Description = f.Description,
            IsActive = f.IsActive,
        }).ToList();
    }

    public async Task<SubscriptionPlanResponse> CreateAsync(
        CreateSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeaturesExistAsync(request.Features, cancellationToken);

        var now = DateTime.UtcNow;

        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Currency = request.Currency,
            BillingInterval = request.BillingInterval,
            IsActive = true,
            IsCustom = true,
            CreatedAt = now,
            UpdatedAt = now,
            PlanFeatures = request.Features
                .Select(f => new PlanFeature { FeatureId = f.FeatureId, Limit = f.Limit })
                .ToList(),
        };

        var created = await _repository.CreateAsync(plan, cancellationToken);

        return MapPlan(created);
    }

    public async Task<SubscriptionPlanResponse> UpdateAsync(
        Guid id,
        UpdateSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureFeaturesExistAsync(request.Features, cancellationToken);

        var features = request.Features
            .Select(f => new PlanFeature { FeatureId = f.FeatureId, Limit = f.Limit })
            .ToList();

        var updated = await _repository.UpdateAsync(
            id,
            request.Name,
            request.Description,
            request.Price,
            request.Currency,
            request.BillingInterval,
            request.IsActive,
            features,
            cancellationToken)
            ?? throw new SubscriptionPlanNotFoundException();

        return MapPlan(updated);
    }

    public async Task<SubscriptionPlanResponse> SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var updated = await _repository.SetActiveAsync(id, isActive, cancellationToken)
            ?? throw new SubscriptionPlanNotFoundException();

        return MapPlan(updated);
    }

    private async Task EnsureFeaturesExistAsync(List<PlanFeatureRequest> features, CancellationToken cancellationToken)
    {
        if (features.Count == 0)
        {
            return;
        }

        var requestedIds = features.Select(f => f.FeatureId).Distinct().ToList();
        var existingIds = await _repository.GetExistingFeatureIdsAsync(requestedIds, cancellationToken);

        if (existingIds.Count != requestedIds.Count)
        {
            throw new UnknownPlanFeatureException();
        }
    }

    private static SubscriptionPlanResponse MapPlan(SubscriptionPlan plan) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        Description = plan.Description,
        Price = plan.Price,
        Currency = plan.Currency,
        BillingInterval = plan.BillingInterval.ToString(),
        IsActive = plan.IsActive,
        IsCustom = plan.IsCustom,
    };

    private static SubscriptionPlanDetailResponse MapDetail(SubscriptionPlan plan, int activeSubscriberCount) => new()
    {
        Id = plan.Id,
        Name = plan.Name,
        Description = plan.Description,
        Price = plan.Price,
        Currency = plan.Currency,
        BillingInterval = plan.BillingInterval.ToString(),
        IsActive = plan.IsActive,
        IsCustom = plan.IsCustom,
        ActiveSubscriberCount = activeSubscriberCount,
        Features = plan.PlanFeatures
            .Select(pf => new PlanFeatureItemResponse
            {
                FeatureKey = pf.Feature.Key,
                FeatureName = pf.Feature.Name,
                Limit = pf.Limit,
            })
            .ToList(),
    };
}
