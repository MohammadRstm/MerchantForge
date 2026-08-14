namespace MerchForge.api.Models;

public class SubscriptionPlan
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = "USD";

    public BillingInterval BillingInterval { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<PlanFeature> PlanFeatures { get; set; }
        = new List<PlanFeature>();

    public ICollection<Subscription> Subscriptions { get; set; }
        = new List<Subscription>();
}