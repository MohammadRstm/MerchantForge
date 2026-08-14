namespace MerchForge.api.Models;

public class BusinessFeatureOverride
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid FeatureId { get; set; }

    public bool IsEnabled { get; set; }

    public int? Limit { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Business Business { get; set; } = null!;

    public Feature Feature { get; set; } = null!;
}