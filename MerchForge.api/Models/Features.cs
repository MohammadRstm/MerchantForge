namespace MerchForge.api.Models;

public class Feature
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<PlanFeature> PlanFeatures { get; set; }
        = new List<PlanFeature>();

    public ICollection<BusinessFeatureOverride> BusinessOverrides { get; set; }
        = new List<BusinessFeatureOverride>();
}