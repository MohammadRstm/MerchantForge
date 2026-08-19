namespace MerchForge.api.DTOs.Auth
{
    public class RegisterSuperAdminRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set;  } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
