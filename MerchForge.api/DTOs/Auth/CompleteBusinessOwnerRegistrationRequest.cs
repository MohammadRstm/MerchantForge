namespace MerchForge.api.DTOs.Auth
{
    public class CompleteBusinessOwnerRegistrationRequest
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string BusinessName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string InvitationToken { get; set; } = null!;

        /// <summary>
        /// The business vertical this store operates in. Required: without a domain
        /// a business has no categories, and without categories it cannot have
        /// products, so this needs to be set at the earliest useful point rather
        /// than left for the owner to configure later in a settings page that
        /// doesn't exist yet.
        /// </summary>
        public Guid BusinessDomainId { get; set; }

        /// <summary>
        /// Category names that don't already exist as platform categories in the
        /// chosen domain. Each becomes a new Category row scoped to this business
        /// (Category.BusinessId set) — usable by this business, but never suggested
        /// to another business owner completing registration later. Optional: most
        /// businesses will find what they need in the domain's existing categories.
        /// </summary>
        public List<string> NewCategoryNames { get; set; } = [];
    }
}
