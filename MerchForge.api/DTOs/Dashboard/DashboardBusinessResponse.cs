namespace MerchForge.api.DTOs.Dashboard;

public class DashboardBusinessResponse
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string OwnerFullName { get; set; } = string.Empty;

    public string OwnerEmail { get; set; } = string.Empty;

    public int MemberCount { get; set; }

    public int ProductCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
