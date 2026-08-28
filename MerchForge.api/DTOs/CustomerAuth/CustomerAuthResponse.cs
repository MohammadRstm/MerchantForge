namespace MerchForge.api.DTOs.CustomerAuth;

public class CustomerAuthResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAt { get; set; }
}
