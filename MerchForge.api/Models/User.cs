using MerchForge.api.Enums;

namespace MerchForge.api.Models;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public SystemRole SystemRole { get; set; } = SystemRole.User;

    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }


    // Navigation properties
    public ICollection<BusinessUser> BusinessMemberships { get; set; }
        = new List<BusinessUser>();

    public ICollection<RefreshToken> RefreshTokens { get; set; }
    = new List<RefreshToken>();
}