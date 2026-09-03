namespace MerchForge.api.DTOs.Auth
{
    public class CompleteBusinessOwnerRegistrationRequest
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string BusinessName { get; set; } = null!;

        public string Email { get; set; } = null!;

        /// <summary>
        /// Chosen by the owner on the accept-invitation form itself — the backend
        /// never generates a password on their behalf.
        /// </summary>
        public string Password { get; set; } = null!;

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

        /// <summary>
        /// Keys of the optional product fields this business wants, chosen from the
        /// domain's catalogue (GET /api/domains/{id}/product-attributes). Snapshotted
        /// into Business.MetadataShape, which then drives what the product form asks
        /// for beyond title/description/price/image.
        ///
        /// Keys rather than ids: the key is what ends up in the shape and in every
        /// product's metadata, so it's the identifier that actually matters.
        /// Optional — a business can run on the fixed fields alone.
        /// </summary>
        public List<string> SelectedProductAttributeKeys { get; set; } = [];

        /// <summary>
        /// Must be true. Enforced by CompleteBusinessOwnerRegistrationRequestValidator,
        /// not just the frontend checkbox — a direct API call with this omitted or
        /// false is rejected the same as the form would refuse to submit it.
        /// </summary>
        public bool AgreedToTerms { get; set; }
    }
}
