using MerchForge.api.DTOs.Invitations;

namespace MerchForge.api.Services.Invitation.interfaces
{
    public interface IInvitationService
    {
        Task<InvitationResponse> CreateBusinessOwnerInvitationAsync(
            CreateBusinessOwnerInvitationRequest request,
            Guid createdByUserId,
            CancellationToken cancellationToken = default);

        string HashInvitationToken(string token);

        Task<Models.Invitation?> GetInvitationByHashToken(string hashToken, CancellationToken cancellationToken = default);

        void ValidateBusinessOwnerInvitation(Models.Invitation? invitation);
    }
}
