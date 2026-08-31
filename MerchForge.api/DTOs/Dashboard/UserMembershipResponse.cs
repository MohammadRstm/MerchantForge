namespace MerchForge.api.DTOs.Dashboard;

public class UserMembershipResponse
{
    public Guid BusinessId { get; set; }

    public string BusinessName { get; set; } = string.Empty;

    public string BusinessRole { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; }
}
