using Hangfire;
using MerchForge.api.Enums;
using MerchForge.api.Jobs.Email;
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
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<RenewSubscriptionPeriodsJob> _logger;

    public RenewSubscriptionPeriodsJob(
        ISubscriptionRepository subscriptionRepository,
        IFeatureCreditService featureCreditService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<RenewSubscriptionPeriodsJob> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _featureCreditService = featureCreditService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var dueSubscriptions = await _subscriptionRepository.GetSubscriptionsDueForRenewalAsync(now, cancellationToken);

        foreach (var subscription in dueSubscriptions)
        {
            try
            {
                if (subscription.CancelAtPeriodEnd)
                {
                    // Atomic claim, same reasoning as the renewal branch below: only
                    // ends it if it's still Active, so a retry re-processing a
                    // subscription an earlier attempt already ended is a safe no-op
                    // rather than a duplicate cancellation/notification.
                    var ended = await _subscriptionRepository.TryEndSubscriptionAsync(
                        subscription.Id, now, cancellationToken);

                    if (!ended)
                    {
                        continue;
                    }

                    _backgroundJobClient.Enqueue<NotifyAdminToTakeWebsiteDownJob>(
                        job => job.ExecuteAsync(subscription.BusinessId));

                    _logger.LogInformation(
                        "Subscription {SubscriptionId} for business {BusinessId} ended at the owner's request; not renewing.",
                        subscription.Id,
                        subscription.BusinessId);

                    continue;
                }

                var newPeriodStart = subscription.CurrentPeriodEnd;
                var newPeriodEnd = subscription.SubscriptionPlan.BillingInterval == BillingInterval.Yearly
                    ? newPeriodStart.AddYears(1)
                    : newPeriodStart.AddMonths(1);

                // Atomic claim-and-advance: only succeeds if CurrentPeriodEnd still
                // matches what was read above, the same pattern
                // FeatureCreditRepository.TryConsumeCreditAsync uses for its balance
                // update. This is what makes a Hangfire retry safe - a subscription
                // an earlier attempt already advanced fails this condition and is
                // skipped instead of being renewed (and re-granted credits) twice.
                var advanced = await _subscriptionRepository.TryAdvanceSubscriptionPeriodAsync(
                    subscription.Id, subscription.CurrentPeriodEnd, newPeriodStart, newPeriodEnd, now, cancellationToken);

                if (!advanced)
                {
                    _logger.LogInformation(
                        "Subscription {SubscriptionId} was already advanced by an earlier attempt; skipping.",
                        subscription.Id);

                    continue;
                }

                await _featureCreditService.ResetImageEditingCreditsForPeriodAsync(
                    subscription.BusinessId, subscription.SubscriptionPlanId, cancellationToken);

                _logger.LogInformation(
                    "Renewed subscription {SubscriptionId} for business {BusinessId}. New period: {Start} - {End}.",
                    subscription.Id,
                    subscription.BusinessId,
                    newPeriodStart,
                    newPeriodEnd);
            }
            catch (Exception ex)
            {
                // One bad subscription must not block the rest of the batch, and
                // must not fail the whole hourly run - each subscription's renewal
                // is independently atomic (see above), so a failure here has left no
                // partial state for this one to worry about on the next run.
                _logger.LogError(
                    ex,
                    "Failed to process renewal for subscription {SubscriptionId} (business {BusinessId}); continuing with the rest of the batch.",
                    subscription.Id,
                    subscription.BusinessId);
            }
        }
    }
}
