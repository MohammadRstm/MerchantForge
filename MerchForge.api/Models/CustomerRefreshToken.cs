namespace MerchForge.api.Models;

/// <summary>
/// Kept as its own table/type rather than a nullable CustomerId column on the existing
/// RefreshTokens table — same reasoning as Customer itself: zero shared surface with the
/// business-owner identity system, so a customer session can never be structurally
/// confused with a dashboard session even by a future bug. Same hash-and-rotate pattern
/// as RefreshToken/RefreshTokenService.
/// </summary>
public class CustomerRefreshToken
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
}
