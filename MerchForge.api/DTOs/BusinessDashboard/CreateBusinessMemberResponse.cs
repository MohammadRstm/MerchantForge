namespace MerchForge.api.DTOs.BusinessDashboard;

/// <summary>
/// The created team member, plus the password generated for them.
///
/// The password is returned exactly once, on creation, because it is never stored in
/// a readable form — only its hash is. The owner has to pass it on, which is why the
/// dashboard shows it in a dismissable panel rather than a toast that disappears.
/// </summary>
public class CreateBusinessMemberResponse
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public DateTime JoinedAt { get; set; }

    public string RawPassword { get; set; } = string.Empty;
}
