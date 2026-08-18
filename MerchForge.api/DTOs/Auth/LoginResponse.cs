namespace MerchForge.api.DTOs.Auth
{
    public class LoginResponse
    {
        public AuthResponse AuthResponse { get; set; }
        public BusinessUserResponse business { get; set; }
    }
}
