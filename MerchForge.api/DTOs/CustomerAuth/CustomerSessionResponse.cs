namespace MerchForge.api.DTOs.CustomerAuth;

/// <summary>
/// Returned by every endpoint that establishes or renews a customer's identity
/// (signup/login/refresh/silent/exchange). ExchangeCode is only ever populated by
/// signup/login, and only when the request carried a ReturnUrl — it is the one-time
/// code the platform's login page hands back to the SDK so it can redirect to
/// {returnUrl}?exchangeCode=... Never populated by refresh/silent/exchange itself.
/// </summary>
public class CustomerSessionResponse
{
    public CustomerAuthResponse AuthResponse { get; set; } = null!;

    public Guid CustomerId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? ExchangeCode { get; set; }
}
