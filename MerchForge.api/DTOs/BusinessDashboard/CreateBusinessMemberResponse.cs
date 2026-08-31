namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// The created team member. The account has no usable password yet - an
/// invitation to set one was emailed to them directly (see
/// IInvitationService.CreateBusinessMemberInvitationAsync), so nothing here can be
/// used to sign in on their behalf.
/// </summary>
public class CreateBusinessMemberResponse
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; }
}
