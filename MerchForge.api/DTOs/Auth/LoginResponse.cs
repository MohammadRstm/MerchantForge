using MerchForge.api.DTOs.Auth;
using MerchForge.api.Enums;

public class LoginResponse
{
    public AuthResponse AuthResponse { get; set; } = null!;

    public Guid UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string SystemRole { get; set; }

    public BusinessContextResponse? Business { get; set; }
}