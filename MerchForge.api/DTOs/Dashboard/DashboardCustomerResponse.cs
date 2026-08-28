namespace MerchForge.api.DTOs.Dashboard;

public class DashboardCustomerResponse
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int OrderCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
