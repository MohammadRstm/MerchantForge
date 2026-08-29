using Hangfire;
using MerchForge.api.Data;
using MerchForge.api.Enums;
using MerchForge.api.Services.Email.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Jobs.Email;

/// <summary>
/// Notifies every SuperAdmin that a business's subscription has ended without
/// renewing and its website should be taken down. Enqueued by
/// RenewSubscriptionPeriodsJob the moment it terminates a CancelAtPeriodEnd
/// subscription - there's no automated deploy/takedown mechanism anywhere in
/// this codebase, so a human has to act on this email.
/// </summary>
public class NotifyAdminToTakeWebsiteDownJob
{
    private readonly MerchForgeDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotifyAdminToTakeWebsiteDownJob> _logger;

    public NotifyAdminToTakeWebsiteDownJob(
        MerchForgeDbContext db,
        IEmailService emailService,
        ILogger<NotifyAdminToTakeWebsiteDownJob> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(Guid businessId)
    {
        var business = await _db.Businesses
            .Where(b => b.Id == businessId)
            .Select(b => new { b.Name, b.WebsiteUrl })
            .FirstOrDefaultAsync();

        if (business is null)
        {
            _logger.LogWarning(
                "Business {BusinessId} was not found. Take-website-down notification skipped.",
                businessId);
            return;
        }

        // Nothing to take down - the business never had a live site.
        if (string.IsNullOrWhiteSpace(business.WebsiteUrl))
        {
            return;
        }

        var adminEmails = await _db.Users
            .Where(u => _db.SystemRoles.Any(r => r.Id == u.SystemRoleId && r.Role == SystemRole.SuperAdmin))
            .Select(u => u.Email)
            .ToListAsync();

        foreach (var adminEmail in adminEmails)
        {
            try
            {
                await _emailService.SendTakeWebsiteDownNotificationAsync(
                    adminEmail,
                    business.Name,
                    business.WebsiteUrl);
            }
            catch (Exception ex)
            {
                // One admin's mailbox rejecting the message shouldn't stop the rest
                // from being notified.
                _logger.LogError(
                    ex,
                    "Failed to notify {AdminEmail} to take down business {BusinessId}'s website.",
                    adminEmail,
                    businessId);
            }
        }
    }
}
