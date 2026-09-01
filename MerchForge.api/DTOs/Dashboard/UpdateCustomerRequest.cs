namespace MerchForge.api.DTOs.Dashboard;

/// <summary>Name and phone only - never email or anything authentication-related, which stays out of Super Admin's reach entirely.</summary>
public class UpdateCustomerRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }
}
