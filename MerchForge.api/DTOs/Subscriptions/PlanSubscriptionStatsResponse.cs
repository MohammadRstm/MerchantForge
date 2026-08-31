namespace MerchForge.api.DTOs.Subscriptions;

public class PlanSubscriptionStatsResponse
{
    /// <summary>Distinct plan tiers (grouped by Name), not raw SubscriptionPlan rows.</summary>
    public int TotalPlans { get; set; }

    /// <summary>Tiers with at least one active (Monthly or Yearly) interval.</summary>
    public int ActivePlans { get; set; }

    /// <summary>Businesses with a currently-Active subscription, platform-wide.</summary>
    public int SubscribedBusinesses { get; set; }

    public int MonthlySubscriptions { get; set; }

    public int YearlySubscriptions { get; set; }
}
