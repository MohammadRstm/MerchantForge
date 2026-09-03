namespace MerchForge.api.DTOs.CustomerAuth;

public class CustomerSignupRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Present only when signup was reached from a storefront's "sign in" action —
    /// when set, the response carries a one-time ExchangeCode for that exact URL.
    /// Absent for a customer who signs up directly on the platform.
    /// </summary>
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Must be true. Enforced by CustomerSignupRequestValidator, not just the
    /// frontend checkbox that sets it — a direct API call with this omitted or false
    /// is rejected the same as the form would refuse to submit it.
    /// </summary>
    public bool AgreedToTerms { get; set; }
}
