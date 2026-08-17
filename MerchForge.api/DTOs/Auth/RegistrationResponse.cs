namespace MerchForge.api.DTOs.Auth
{
    public class RegistrationResponse
    {
        public AuthResponse AuthResponse { get; set; }
        public string rawPassword { get; set; }
    }
}
