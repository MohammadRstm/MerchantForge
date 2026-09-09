using Hangfire;
using MerchForge.api.Data;
using MerchForge.api.Enums;
using MerchForge.api.Services.Email.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Jobs.Email;

/// <summary>
/// Delivers a public contact-form enquiry to every SuperAdmin.
///
/// Queued rather than sent inline, like every other email here: the visitor
/// gets their confirmation the moment the enquiry is accepted, instead of
/// waiting on an SMTP round trip and being shown a failure for something that
/// was actually received and is merely slow to forward.
///
/// It takes the enquiry as arguments rather than an id because nothing is
/// persisted - there is no contact table, and adding one would mean storing
/// unsolicited personal data from anonymous senders for no purpose the product
/// has. Hangfire serialises these into its own job store, which is the only
/// place an enquiry lives, and only until it is delivered and expires.
/// </summary>
public class SendContactEnquiryJob
{
    private readonly MerchForgeDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendContactEnquiryJob> _logger;

    public SendContactEnquiryJob(
        MerchForgeDbContext db,
        IEmailService emailService,
        ILogger<SendContactEnquiryJob> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(
        string senderName,
        string senderEmail,
        string subject,
        string message)
    {
        var adminEmails = await _db.Users
            .Where(u => _db.SystemRoles.Any(r => r.Id == u.SystemRoleId && r.Role == SystemRole.SuperAdmin))
            .Select(u => u.Email)
            .ToListAsync();

        if (adminEmails.Count == 0)
        {
            // Worth an error rather than a warning: the form is accepting
            // enquiries and telling people they have been received, and there is
            // nobody to receive them. Nothing else in the system would show it.
            _logger.LogError(
                "A contact enquiry from {SenderEmail} could not be delivered: no SuperAdmin accounts exist.",
                senderEmail);
            return;
        }

        foreach (var adminEmail in adminEmails)
        {
            try
            {
                await _emailService.SendContactEnquiryNotificationAsync(
                    adminEmail,
                    senderName,
                    senderEmail,
                    subject,
                    message);
            }
            catch (Exception ex)
            {
                // Swallowed per-recipient on purpose: letting this escape would
                // have Hangfire retry the whole job and re-deliver the enquiry to
                // every admin whose mailbox already accepted it.
                _logger.LogError(
                    ex,
                    "Failed to forward a contact enquiry from {SenderEmail} to {AdminEmail}.",
                    senderEmail,
                    adminEmail);
            }
        }
    }
}
