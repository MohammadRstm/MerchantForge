using Hangfire;
using MerchForge.api.Enums;
using MerchForge.api.Repositories.Interfaces;
using MerchForge.api.Services.Subscription.interfaces;

namespace MerchForge.api.Jobs.Subscriptions;

/// <summary>
/// Rolls forward every Active subscription whose current billing period has
/// ended, and resets its ai.image_editing credits to the plan's per-period
/// allotment. No real payment provider exists yet, so this treats every period
/// as auto-renewed for free - this is the seam a real payment integration will
/// need to hook into: before rolling the period forward and resetting credits,
/// it must first charge the business and only proceed on success (or mark the
/// subscription PastDue/Expired on failure) instead of unconditionally renewing,
/// matching the same stub-seam pattern FeatureCreditService.PurchaseAsync already
/// documents for standalone credit purchases.
/// </summary>
public class RenewSubscriptionPeriodsJob
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IFeatureCreditService _featureCreditService;
    private readonly ILogger<RenewSubscriptionPeriodsJob> _logger;

    public RenewSubscriptionPeriodsJob(
        ISubscriptionRepository subscriptionRepository,
        IFeatureCreditService featureCreditService,
        ILogger<RenewSubscriptionPeriodsJob> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _featureCreditService = featureCreditService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dueSubscriptions = await _subscriptionRepository.GetSubscriptionsDueForRenewalAsync(now, cancellationToken);

        foreach (var subscription in dueSubscriptions)
        {
            subscription.CurrentPeriodStart = subscription.CurrentPeriodEnd;
            subscription.CurrentPeriodEnd = subscription.SubscriptionPlan.BillingInterval == BillingInterval.Yearly
                ? subscription.CurrentPeriodStart.AddYears(1)
                : subscription.CurrentPeriodStart.AddMonths(1);
            subscription.UpdatedAt = now;

            await _featureCreditService.ResetImageEditingCreditsForPeriodAsync(
                subscription.BusinessId, subscription.SubscriptionPlanId, cancellationToken);

            _logger.LogInformation(
                "Renewed subscription {SubscriptionId} for business {BusinessId}. New period: {Start} - {End}.",
                subscription.Id,
                subscription.BusinessId,
                subscription.CurrentPeriodStart,
                subscription.CurrentPeriodEnd);
        }

        await _subscriptionRepository.SaveChangesAsync(cancellationToken);
    }
}
