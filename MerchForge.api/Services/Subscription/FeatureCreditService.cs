using MerchForge.api.DTOs.Subscriptions;
using MerchForge.api.Exceptions.Subscriptions;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Subscription.interfaces;

namespace MerchForge.api.Services.Subscription;

public class FeatureCreditService : IFeatureCreditService
{
    private readonly IFeatureCreditRepository _repository;
    private readonly ISubscriptionService _subscriptionService;

    public FeatureCreditService(
        IFeatureCreditRepository repository,
        ISubscriptionService subscriptionService)
    {
        _repository = repository;
        _subscriptionService = subscriptionService;
    }

    public async Task<List<FeatureCreditOverviewResponse>> GetOverviewAsync(
        Guid businessId,
        CancellationToken cancellationToken = default)
    {
        var features = await _repository.GetPurchasableFeaturesAsync(cancellationToken);
        var balances = await _repository.GetBalancesForBusinessAsync(businessId, cancellationToken);
        var balancesByFeature = balances.ToDictionary(b => b.FeatureId);

        var overview = new List<FeatureCreditOverviewResponse>();

        foreach (var feature in features)
        {
            var balance = balancesByFeature.GetValueOrDefault(feature.Id);

            overview.Add(new FeatureCreditOverviewResponse
            {
                FeatureKey = feature.Key,
                FeatureName = feature.Name,
                FeatureDescription = feature.Description,
                IncludedInPlan = await _subscriptionService.HasPlanFeatureAsync(businessId, feature.Key),
                CreditsRemaining = balance?.CreditsRemaining ?? 0,
                CreditsGrantedTotal = balance?.CreditsGrantedTotal ?? 0,
                Packages = feature.CreditPackages
                    .Select(p => new FeatureCreditPackageResponse
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Credits = p.Credits,
                        Price = p.Price,
                        Currency = p.Currency,
                    })
                    .ToList(),
            });
        }

        return overview;
    }

    public async Task<BusinessFeatureCreditResponse> PurchaseAsync(
        Guid businessId,
        Guid packageId,
        CancellationToken cancellationToken = default)
    {
        var package = await _repository.GetPackageAsync(packageId, cancellationToken);

        if (package is null || !package.IsActive || !package.Feature.IsActive || !package.Feature.SupportsCreditPurchase)
        {
            throw new FeatureCreditPackageNotFoundException();
        }

        // No real charge yet - the owner has already confirmed "pay" client-side and
        // there is nowhere else in the codebase a payment provider is wired in. This
        // is the seam a real charge will go through before granting credits.
        var balance = await _repository.GrantCreditsAsync(
            businessId,
            package.FeatureId,
            package.Id,
            package.Credits,
            cancellationToken);

        return new BusinessFeatureCreditResponse
        {
            FeatureKey = package.Feature.Key,
            CreditsRemaining = balance.CreditsRemaining,
            CreditsGrantedTotal = balance.CreditsGrantedTotal,
        };
    }

    public async Task<bool> TryConsumeAsync(
        Guid businessId,
        string featureKey,
        string? reference,
        CancellationToken cancellationToken = default)
    {
        // Bundled in the plan means unlimited - nothing to meter.
        if (await _subscriptionService.HasPlanFeatureAsync(businessId, featureKey))
        {
            return true;
        }

        return await _repository.TryConsumeCreditAsync(businessId, featureKey, reference, cancellationToken);
    }
}
