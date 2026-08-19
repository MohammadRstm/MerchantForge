namespace MerchForge.api.DTOs.BusinessDashboard;

public class BusinessSubscriptionResponse
{
    public Guid Id { get; set; }

    public string PlanName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string BillingInterval { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CurrentPeriodStart { get; set; }

    public DateTime CurrentPeriodEnd { get; set; }

    public List<PlanFeatureItemResponse> Features { get; set; } = new();
}
