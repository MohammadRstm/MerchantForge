namespace MerchForge.api.Models;

/// <summary>
/// A shopper's identity, shared across every storefront MerchForge serves. Deliberately
/// has no SystemRoleId and no business link of any kind — a Customer can never be
/// confused with a User (business owner/staff/SuperAdmin), structurally, not just by
/// convention. Address fields are nullable and exist purely as checkout prefill; Order
/// already snapshots its own shipping fields independently, so nothing here is ever the
/// source of truth for a placed order.
/// </summary>
public class Customer
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CustomerRefreshToken> RefreshTokens { get; set; } = new List<CustomerRefreshToken>();
}
