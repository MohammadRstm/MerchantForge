using MerchForge.api.Enums;

namespace MerchForge.api.Models;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public Guid SystemRoleId { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Null = active. Set = disabled, and records when. The user cannot
    // authenticate while this is set (checked in AuthService.LoginAsync).
    public DateTime? DisabledAt { get; set; }

    // The Super Admin who disabled this account, for accountability. Cleared
    // together with DisabledAt on re-enable, so it never outlives the state it describes.
    public Guid? DisabledByUserId { get; set; }

    // Navigation properties
    public ICollection<BusinessUser> BusinessMemberships { get; set; }
        = new List<BusinessUser>();

    public ICollection<RefreshToken> RefreshTokens { get; set; }
    = new List<RefreshToken>();

    public User? DisabledByUser { get; set; }
}