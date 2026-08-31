namespace MerchForge.api.DTOs.Auth
{
    /// <summary>
    /// A team member setting their own password after being invited by their
    /// business owner. No name/email/business fields — the account already exists
    /// (created at invite time by BusinessMemberService.CreateMemberAsync) and the
    /// email comes from the invitation itself, not the request.
    /// </summary>
    public class CompleteBusinessMemberRegistrationRequest
    {
        public string InvitationToken { get; set; } = null!;

        public string Password { get; set; } = null!;
    }
}
