using MerchForge.api.DTOs.Audit;

namespace MerchForge.api.DTOs.Dashboard;

public class DashboardUserDetailResponse
{
    public Guid Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string SystemRole { get; set; } = string.Empty;

    public bool IsDisabled { get; set; }

    public DateTime? DisabledAt { get; set; }

    public string? DisabledByName { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public List<UserMembershipResponse> Memberships { get; set; } = new();

    public bool HasActiveSession { get; set; }

    public int ActiveSessionCount { get; set; }

    public DateTime? NextSessionExpiresAt { get; set; }

    public List<AuditLogResponse> RecentActivity { get; set; } = new();
}
