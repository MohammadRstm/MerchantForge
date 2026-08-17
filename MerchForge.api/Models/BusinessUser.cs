using MerchForge.api.Enums;

namespace MerchForge.api.Models;

public class BusinessUser
{
    public Guid BusinessId { get; set; }

    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties

    public Business Business { get; set; } = null!;

    public User User { get; set; } = null!;
}