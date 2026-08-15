namespace MerchForge.api.DTOs.Invitations
{
    public class InvitationResponse
    {
        public Guid Id { get; set; }

        public string Email { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }
    }
}
