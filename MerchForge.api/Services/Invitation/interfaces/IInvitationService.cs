using MerchForge.api.DTOs.Invitations;

namespace MerchForge.api.Services.Invitation.interfaces
{
    public interface IInvitationService
    {
        Task<InvitationResponse> CreateBusinessOwnerInvitationAsync(
            CreateBusinessOwnerInvitationRequest request,
            Guid createdByUserId,
            CancellationToken cancellationToken = default);
    }
}
