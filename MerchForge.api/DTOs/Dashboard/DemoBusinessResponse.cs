namespace MerchForge.api.DTOs.Dashboard;

public class DemoBusinessResponse
{
    public Guid BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public Guid OwnerUserId { get; set; }

    public string OwnerEmail { get; set; } = string.Empty;

    public int ProductCount { get; set; }

    public int CustomerCount { get; set; }

    public int OrderCount { get; set; }
}
