namespace MerchForge.api.Models;

public class Feature
{
    public Guid Id { get; set; }

    public string Key { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PlanFeature> PlanFeatures { get; set; }
        = new List<PlanFeature>();
}