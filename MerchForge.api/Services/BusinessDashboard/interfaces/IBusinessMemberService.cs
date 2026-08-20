using MerchForge.api.DTOs.BusinessDashboard;

namespace MerchForge.api.Services.BusinessDashboard.interfaces
{
    /// <summary>
    /// Team management for a single business.
    ///
    /// Separate from IBusinessDashboardService because creating an account is an
    /// identity concern — it needs password hashing and role lookups — while the
    /// dashboard service only reads and aggregates. Keeping them apart also keeps
    /// the read path free of dependencies it never uses.
    /// </summary>
    public interface IBusinessMemberService
    {
        /// <summary>
        /// Creates a user account and attaches it to this business as an Admin or a
        /// Member. The returned password is generated here and never stored in
        /// readable form, so this response is the only chance to pass it on.
        /// </summary>
        Task<CreateBusinessMemberResponse> CreateMemberAsync(
            Guid businessId,
            CreateBusinessMemberRequest request,
            CancellationToken cancellationToken = default);
    }
}
