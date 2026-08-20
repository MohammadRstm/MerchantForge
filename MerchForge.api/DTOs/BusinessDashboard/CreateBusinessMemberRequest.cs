using MerchForge.api.Enums;

namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// An owner adding someone to their team directly — the account is created and
/// attached in one step rather than invited.
///
/// Carries no business id: that comes from the route, which the BusinessOwner policy
/// has already checked the caller owns. Accepting it from the body would let an owner
/// add members to a business that isn't theirs.
/// </summary>
public class CreateBusinessMemberRequest
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Admin or Member. Owner is rejected — a business has exactly one.</summary>
    public BusinessRole Role { get; set; }
}
