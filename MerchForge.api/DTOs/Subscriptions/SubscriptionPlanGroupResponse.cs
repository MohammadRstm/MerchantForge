using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.DTOs.Subscriptions;

/// <summary>
/// One row per plan interval within a tier - Monthly and Yearly are genuinely
/// separate SubscriptionPlan database rows (same Name, own Id/Price/IsActive),
/// grouped here for display only. Null when that tier doesn't offer this interval.
/// </summary>
public class SubscriptionPlanGroupIntervalResponse
{
    public Guid Id { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Businesses with an Active subscription on this specific row.</summary>
    public int ActiveSubscriberCount { get; set; }
}

/// <summary>
/// A plan tier (e.g. "Starter"), merging its Monthly and Yearly rows for display.
/// The two intervals remain independently editable/deactivatable - see
/// SubscriptionPlanGroupIntervalResponse.
/// </summary>
public class SubscriptionPlanGroupResponse
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Currency { get; set; } = string.Empty;

    public bool IsCustom { get; set; }

    public SubscriptionPlanGroupIntervalResponse? Monthly { get; set; }

    public SubscriptionPlanGroupIntervalResponse? Yearly { get; set; }

    /// <summary>Sum of Monthly + Yearly active subscriber counts.</summary>
    public int TotalActiveSubscriberCount { get; set; }

    /// <summary>
    /// This tier's share of every currently-active subscription platform-wide -
    /// null when there are no active subscriptions at all (avoids a divide-by-zero
    /// reading as "0%").
    /// </summary>
    public decimal? PercentOfActiveSubscriptions { get; set; }

    /// <summary>
    /// From whichever interval has features configured (Monthly preferred) - in
    /// practice both intervals of a tier are kept in sync by convention, but nothing
    /// enforces that, so this reflects one interval's actual configuration rather
    /// than pretending to merge two potentially-different feature lists.
    /// </summary>
    public List<PlanFeatureItemResponse> Features { get; set; } = new();
}
