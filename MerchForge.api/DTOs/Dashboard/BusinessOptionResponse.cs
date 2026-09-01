namespace MerchForge.api.DTOs.Dashboard;

/// <summary>The cheap {id, name} shape for populating a business picker/filter - never the full paginated business list.</summary>
public class BusinessOptionResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
