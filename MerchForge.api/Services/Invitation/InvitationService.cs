using MerchForge.api.Data;
using MerchForge.api.DTOs.Invitations;
using MerchForge.api.Enums;
using MerchForge.api.Models;
using MerchForge.api.Services.Email.Interfaces;
using MerchForge.api.Services.Invitation.interfaces;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Services.Invitation
{
    public class InvitationService : IInvitationService
    {
        private readonly MerchForgeDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;


        public InvitationService(MerchForgeDbContext db , IConfiguration configuration, IEmailService emailService)
        {
            _db = db;
            _configuration = configuration;
            _emailService = emailService;
        }

        public async Task<InvitationResponse> CreateBusinessOwnerInvitationAsync(
            CreateBusinessOwnerInvitationRequest request,
            Guid createdByUserId,
            CancellationToken cancellationToken = default)
        {
            var email = request.Email;

            // Revoke any previous pending invitation for this email.
            var existingInvitations = await _db.Invitations
                .Where(i =>
                    i.Email == email &&
                    i.Type == InvitationType.BusinessOwner &&
                    i.AcceptedAt == null &&
                    i.RevokedAt == null &&
                    i.ExpiresAt > DateTime.UtcNow)
                .ToListAsync(cancellationToken);

            foreach (var existingInvitation in existingInvitations)
            {
                existingInvitation.RevokedAt = DateTime.UtcNow;
            }

            var rawToken = GenerateInvitationToken();

            // the hash is stored in the database.
            var tokenHash = HashInvitationToken(rawToken);

            var now = DateTime.UtcNow;
            var expiresAt = now.AddHours(48);

            var invitation = new Models.Invitation
            {
                Id = Guid.NewGuid(),

                Email = email,

                TokenHash = tokenHash,

                Type = InvitationType.BusinessOwner,

                BusinessId = null,

                BusinessRole = BusinessRole.Owner,

                SystemRole = SystemRole.User,

                CreatedByUserId = createdByUserId,

                CreatedAt = now,

                ExpiresAt = expiresAt,

                AcceptedAt = null,

                RevokedAt = null
            };

            await _db.Invitations.AddAsync(
                invitation,
                cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

          
            var invitationLink =
                $"{_configuration["Frontend:BaseUrl"]}/accept-invitation?token={Uri.EscapeDataString(rawToken)}";

            await _emailService.SendBusinessOwnerInvitationAsync(
                email,
                invitationLink,
                expiresAt,
                cancellationToken
            );

            return new InvitationResponse
            {
                Email = email,
                ExpiresAt = expiresAt
            };
        }
        private static string GenerateInvitationToken()
        {
            return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32));
        }

        private static string HashInvitationToken(string token)
        {
            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(bytes);
        }
    }
}
