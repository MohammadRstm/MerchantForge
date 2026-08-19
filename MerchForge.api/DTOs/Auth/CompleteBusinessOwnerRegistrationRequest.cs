namespace MerchForge.api.DTOs.Auth
{
    public class CompleteBusinessOwnerRegistrationRequest
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string BusinessName { get; set; } = null!;

        public string Email { get; set; }  = null!;

        public string InvitationToken { get; set; } = null!;

    }
}
