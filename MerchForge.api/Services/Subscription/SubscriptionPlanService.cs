using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Common;
using MerchForge.api.DTOs.Subscriptions;
using MerchForge.api.Enums;
using MerchForge.api.Exceptions.Subscriptions;
using MerchForge.api.Models;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Audit.interfaces;
using MerchForge.api.Services.Common;
using MerchForge.api.Services.Subscription.interfaces;

namespace MerchForge.api.Services.Subscription;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly ISubscriptionPlanRepository _repository;
    private readonly IAuditLogService _auditLogService;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public SubscriptionPlanService(
        ISubscriptionPlanRepository repository,
        IAuditLogService auditLogService,
        ICurrentUserAccessor currentUserAccessor)
    {
        _repository = repository;
        _auditLogService = auditLogService;
        _currentUserAccessor = currentUserAccessor;
    }

    public async Task<List<SubscriptionPlanResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _repository.GetAllAsync(cancellationToken);

        return plans.Select(MapPlan).ToList();
    }

    public async Task<List<SubscriptionPlanGroupResponse>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _repository.GetAllAsync(cancellationToken);
        var subscriberCounts = await _repository.GetActiveSubscriberCountsByPlanIdAsync(cancellationToken);
        var totalActiveSubscriptions = subscriberCounts.Values.Sum();

        return plans
            .GroupBy(p => p.Name)
            .Select(g =>
            {
                var monthly = g.FirstOrDefault(p => p.BillingInterval == BillingInterval.Monthly);
                var yearly = g.FirstOrDefault(p => p.BillingInterval == BillingInterval.Yearly);
                var featuresSource = monthly ?? yearly;

                var monthlyCount = monthly is null ? 0 : subscriberCounts.GetValueOrDefault(monthly.Id);
                var yearlyCount = yearly is null ? 0 : subscriberCounts.GetValueOrDefault(yearly.Id);
                var totalCount = monthlyCount + yearlyCount;

                return new SubscriptionPlanGroupResponse
                {
                    Name = g.Key,
                    Description = featuresSource?.Description,
                    Currency = featuresSource?.Currency ?? "USD",
                    IsCustom = g.Any(p => p.IsCustom),
                    Monthly = monthly is null ? null : new SubscriptionPlanGroupIntervalResponse
                    {
                        Id = monthly.Id,
                        Price = monthly.Price,
                        IsActive = monthly.IsActive,
                        ActiveSubscriberCount = monthlyCount,
                    },
                    Yearly = yearly is null ? null : new SubscriptionPlanGroupIntervalResponse
                    {
                        Id = yearly.Id,
                        Price = yearly.Price,
                        IsActive = yearly.IsActive,
                        ActiveSubscriberCount = yearlyCount,
                    },
                    TotalActiveSubscriberCount = totalCount,
                    PercentOfActiveSubscriptions = totalActiveSubscriptions > 0
                        ? Math.Round(100m * totalCount / totalActiveSubscriptions, 1)
                        : null,
                    Features = featuresSource is null
                        ? new List<PlanFeatureItemResponse>()
                        : featuresSource.PlanFeatures
                            .Select(pf => new PlanFeatureItemResponse
                            {
                                FeatureKey = pf.Feature.Key,
                                FeatureName = pf.Feature.Name,
                                FeatureDescription = pf.Feature.Description,
                                Limit = pf.Limit,
                            })
                            .ToList(),
                };
            })
            .OrderBy(g => g.Monthly?.Price ?? g.Yearly?.Price ?? 0)
            .ToList();
    }

    public async Task<List<KeyCountResponse>> GetDistributionAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetActiveSubscriberCountsByPlanNameAsync(cancellationToken);
    }

    public async Task<PlanSubscriptionStatsResponse> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _repository.GetAllAsync(cancellationToken);
        var intervalCounts = await _repository.GetActiveSubscriptionCountsByBillingIntervalAsync(cancellationToken);

        var tierNames = plans.Select(p => p.Name).Distinct().ToList();
        var activeTierNames = plans.Where(p => p.IsActive).Select(p => p.Name).Distinct().ToList();

        var monthly = intervalCounts.GetValueOrDefault(BillingInterval.Monthly);
        var yearly = intervalCounts.GetValueOrDefault(BillingInterval.Yearly);

        return new PlanSubscriptionStatsResponse
        {
            TotalPlans = tierNames.Count,
            ActivePlans = activeTierNames.Count,
            SubscribedBusinesses = monthly + yearly,
            MonthlySubscriptions = monthly,
            YearlySubscriptions = yearly,
        };
    }

    public async Task<List<SubscriptionPlanDetailResponse>> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _repository.GetActiveAsync(cancellationToken);

        // No subscriber count here - that's an internal metric, not something a
        // public landing/billing page needs.
        return plans.Select(plan => MapDetail(plan, activeSubscriberCount: 0)).ToList();
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

        await _auditLogService.LogAsync(
            AuditEventType.Subscription, "SubscriptionPlanCreated",
            $"Created subscription plan \"{created.Name}\" ({created.BillingInterval}).",
            success: true, actorUserId: _currentUserAccessor.UserId,
            entityType: "SubscriptionPlan", entityId: created.Id,
            cancellationToken: cancellationToken);

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

        await _auditLogService.LogAsync(
            AuditEventType.Subscription, "SubscriptionPlanUpdated",
            $"Updated subscription plan \"{updated.Name}\" ({updated.BillingInterval}).",
            success: true, actorUserId: _currentUserAccessor.UserId,
            entityType: "SubscriptionPlan", entityId: updated.Id,
            cancellationToken: cancellationToken);

        return MapPlan(updated);
    }

    public async Task<SubscriptionPlanResponse> SetActiveAsync(
        Guid id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var updated = await _repository.SetActiveAsync(id, isActive, cancellationToken)
            ?? throw new SubscriptionPlanNotFoundException();

        await _auditLogService.LogAsync(
            AuditEventType.Subscription, isActive ? "SubscriptionPlanReactivated" : "SubscriptionPlanDeactivated",
            $"{(isActive ? "Reactivated" : "Deactivated")} subscription plan \"{updated.Name}\" ({updated.BillingInterval}).",
            success: true, actorUserId: _currentUserAccessor.UserId,
            entityType: "SubscriptionPlan", entityId: updated.Id,
            cancellationToken: cancellationToken);

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
                FeatureDescription = pf.Feature.Description,
                Limit = pf.Limit,
            })
            .ToList(),
    };
}
