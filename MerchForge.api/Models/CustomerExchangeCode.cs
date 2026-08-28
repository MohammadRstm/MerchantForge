namespace MerchForge.api.Models;

/// <summary>
/// Short-lived (60s), single-use handoff from a real first-party platform login back to
/// the storefront that requested it. Storing ReturnUrl and rejecting a redemption whose
/// caller-supplied returnUrl doesn't match is what makes the code non-replayable against
/// a different storefront than the one it was issued for. Never a JWT in the URL —
/// redeeming this over POST is the only way to turn it into a real access token.
/// </summary>
public class CustomerExchangeCode
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string CodeHash { get; set; } = string.Empty;

    public string ReturnUrl { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
}
