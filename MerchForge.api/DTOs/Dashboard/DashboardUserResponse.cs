namespace MerchForge.api.DTOs.Dashboard;

public class DashboardUserResponse
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string SystemRole { get; set; } = string.Empty;

    public string? BusinessName { get; set; }

    public string? BusinessRole { get; set; }

    /// <summary>Memberships beyond the one shown above, e.g. 2 -> "+2 businesses".</summary>
    public int AdditionalMembershipCount { get; set; }

    public bool HasActiveSession { get; set; }

    public bool IsDisabled { get; set; }

    public DateTime CreatedAt { get; set; }
}
