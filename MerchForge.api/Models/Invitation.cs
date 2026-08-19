using MerchForge.api.Enums;

namespace MerchForge.api.Models;

public class Invitation
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public InvitationType Type { get; set; }

    public Guid? BusinessId { get; set; }

    public BusinessRole? BusinessRole { get; set; }

    public SystemRole? SystemRole { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt {  get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime? EmailSentAt { get; set; }

    public DateTime? EmailDeliveryFailedAt { get; set; }

    public string? EmailDeliveryError { get; set; }

    // Navigation properties

    public Business? Business { get; set; }

    public User CreatedByUser { get; set; } = null!;
}