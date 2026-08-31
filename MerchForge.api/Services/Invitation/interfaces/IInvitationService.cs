using MerchForge.api.DTOs.BusinessDashboard;
using MerchForge.api.DTOs.Invitations;

namespace MerchForge.api.Services.Invitation.interfaces
{
    public interface IInvitationService
    {
        Task<InvitationResponse> CreateBusinessOwnerInvitationAsync(
            CreateBusinessOwnerInvitationRequest request,
            Guid createdByUserId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Mints and emails the invitation a newly created team member uses to set
        /// their own password. The user/membership rows already exist by the time
        /// this is called (see BusinessMemberService.CreateMemberAsync) - this only
        /// establishes how they get a usable credential.
        /// </summary>
        Task CreateBusinessMemberInvitationAsync(
            Guid businessId,
            string businessName,
            CreateBusinessMemberRequest request,
            Guid createdByUserId,
            CancellationToken cancellationToken = default);

        string HashInvitationToken(string token);

        Task<Models.Invitation?> GetInvitationByHashToken(string hashToken, CancellationToken cancellationToken = default);

        void ValidateBusinessOwnerInvitation(Models.Invitation? invitation);

        void ValidateBusinessMemberInvitation(Models.Invitation? invitation);
    }
}
