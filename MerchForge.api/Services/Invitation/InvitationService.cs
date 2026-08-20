using MerchForge.api.Data;
using MerchForge.api.DTOs.Invitations;
using MerchForge.api.Enums;
using MerchForge.api.Jobs.Email;
using MerchForge.api.Services.Email.Interfaces;
using MerchForge.api.Services.Invitation.interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using MerchForge.api.Exceptions.Invitation;
using Hangfire;
using System.Text;

namespace MerchForge.api.Services.Invitation
{
    public class InvitationService : IInvitationService
    {
        private readonly MerchForgeDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IBackgroundJobClient _backgroundJobClient;


        public InvitationService(
            MerchForgeDbContext db,
            IConfiguration configuration,
            IEmailService emailService,
            IBackgroundJobClient backgroundJobClient)
        {
            _db = db;
            _configuration = configuration;
            _emailService = emailService;
            _backgroundJobClient = backgroundJobClient;
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

                RevokedAt = null,

                EmailSentAt = null,

                EmailDeliveryError = null,

                EmailDeliveryFailedAt = null,
            };

            await _db.Invitations.AddAsync(
                invitation,
                cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

          
            var invitationLink =
                $"{_configuration["Frontend:BaseUrl"]}/accept-invitation?token={Uri.EscapeDataString(rawToken)}&email={email}";

            _backgroundJobClient.Enqueue<SendBusinessOwnerInvitationJob>(
                job => job.ExecuteAsync(
                    invitation.Id,
                    invitationLink));

            return new InvitationResponse
            {
                Email = email,
                ExpiresAt = expiresAt
            };
        }
        public string HashInvitationToken(string token)
        {
            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(token));

            return Convert.ToHexString(bytes);
        }

        /// <summary>
        /// Null when no invitation carries that token. Deliberately not an exception:
        /// a token that matches nothing is the ordinary case for a mistyped or stale
        /// link, and ValidateBusinessOwnerInvitation already turns null into the same
        /// InvalidInvitationException a revoked or expired one produces. Throwing a
        /// bare Exception here surfaced to the caller as a 500.
        /// </summary>
        public async Task<Models.Invitation?> GetInvitationByHashToken(string hashToken, CancellationToken cancellationToken = default)
        {
            return await _db.Invitations
                .SingleOrDefaultAsync(
                    i => i.TokenHash == hashToken,
                    cancellationToken);
        }

        public void ValidateBusinessOwnerInvitation(Models.Invitation? invitation)
        {
            if (invitation == null)
            {
                throw new InvalidInvitationException();
            }

            if (invitation.Type != InvitationType.BusinessOwner)
            {
                throw new InvalidInvitationException();
            }

            if (invitation.AcceptedAt.HasValue)
            {
                throw new InvitationAlreadyUsedException();
            }

            if (invitation.RevokedAt.HasValue)
            {
                throw new InvitationRevokedException();
            }

            if (invitation.ExpiresAt <= DateTime.UtcNow)
            {
                throw new InvitationExpiredException();
            }

            if (invitation.BusinessRole != BusinessRole.Owner)
            {
                throw new InvalidInvitationException();
            }

            if (invitation.SystemRole != SystemRole.User)
            {
                throw new InvalidInvitationException();
            }
        }
        private static string GenerateInvitationToken()
        {
            return Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32));
        }

    }
}
