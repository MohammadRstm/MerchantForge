namespace MerchForge.api.DTOs.CustomerAuth;

public class CustomerLoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>See CustomerSignupRequest.ReturnUrl.</summary>
    public string? ReturnUrl { get; set; }
}
