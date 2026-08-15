using Hangfire;
using MerchForge.api.Data;
using MerchForge.api.Services.Email.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Jobs.Email;

public class SendBusinessOwnerInvitationJob
{
    private readonly MerchForgeDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendBusinessOwnerInvitationJob> _logger;

    public SendBusinessOwnerInvitationJob(
        MerchForgeDbContext db,
        IEmailService emailService,
        ILogger<SendBusinessOwnerInvitationJob> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(
        Guid invitationId,
        string invitationLink,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _db.Invitations
            .FirstOrDefaultAsync(
                i => i.Id == invitationId,
                cancellationToken);

        if (invitation is null)
        {
            _logger.LogWarning(
                "Invitation {InvitationId} was not found. Email job skipped.",
                invitationId);

            return;
        }

        if (invitation.RevokedAt.HasValue)
        {
            _logger.LogInformation(
                "Invitation {InvitationId} was revoked. Email job skipped.",
                invitationId);

            return;
        }

        if (invitation.AcceptedAt.HasValue)
        {
            _logger.LogInformation(
                "Invitation {InvitationId} was already accepted. Email job skipped.",
                invitationId);

            return;
        }

        if (invitation.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogInformation(
                "Invitation {InvitationId} has expired. Email job skipped.",
                invitationId);

            return;
        }

        try
        {
            await _emailService.SendBusinessOwnerInvitationAsync(
                invitation.Email,
                invitationLink,
                invitation.ExpiresAt,
                cancellationToken);

            invitation.EmailSentAt = DateTime.UtcNow;
            invitation.EmailDeliveryFailedAt = null;
            invitation.EmailDeliveryError = null;

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Invitation email successfully sent for invitation {InvitationId}.",
                invitationId);
        }
        catch (Exception ex)
        {
            invitation.EmailDeliveryFailedAt = DateTime.UtcNow;
            invitation.EmailDeliveryError = ex.Message;

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogError(
                ex,
                "Failed to send invitation email for invitation {InvitationId}.",
                invitationId);

            throw;
        }
    }
}