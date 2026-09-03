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

        /// <summary>
        /// Must be true. Enforced by CompleteBusinessMemberRegistrationRequestValidator,
        /// not just the frontend checkbox — a direct API call with this omitted or
        /// false is rejected the same as the form would refuse to submit it. A team
        /// member's account already exists (created at invite time), but this is the
        /// moment they set their own password and actually start using it, so it's
        /// the right point to record their own acceptance rather than assuming it
        /// from whoever invited them.
        /// </summary>
        public bool AgreedToTerms { get; set; }
    }
}
