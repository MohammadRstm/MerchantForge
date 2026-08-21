using Hangfire;
using MerchForge.api.Data;
using MerchForge.api.Enums;
using MerchForge.api.Services.Email.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Jobs.Email;

/// <summary>
/// Notifies every SuperAdmin when a business chooses a website template — the same
/// role that manages the template catalogue in the first place, so they're the ones
/// who'd need to know a template now has to be deployed for someone.
/// </summary>
public class NotifyAdminOfWebsiteTemplateChoiceJob
{
    private readonly MerchForgeDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotifyAdminOfWebsiteTemplateChoiceJob> _logger;

    public NotifyAdminOfWebsiteTemplateChoiceJob(
        MerchForgeDbContext db,
        IEmailService emailService,
        ILogger<NotifyAdminOfWebsiteTemplateChoiceJob> logger)
    {
        _db = db;
        _emailService = emailService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(Guid businessId, Guid websiteTemplateId)
    {
        var business = await _db.Businesses
            .Where(b => b.Id == businessId)
            .Select(b => new { b.Name })
            .FirstOrDefaultAsync();

        if (business is null)
        {
            _logger.LogWarning(
                "Business {BusinessId} was not found. Website-template-chosen notification skipped.",
                businessId);
            return;
        }

        var template = await _db.WebsiteTemplates
            .Where(t => t.Id == websiteTemplateId)
            .Select(t => new { t.Label, t.Name })
            .FirstOrDefaultAsync();

        if (template is null)
        {
            _logger.LogWarning(
                "WebsiteTemplate {WebsiteTemplateId} was not found. Website-template-chosen notification skipped.",
                websiteTemplateId);
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
                await _emailService.SendWebsiteTemplateChosenNotificationAsync(
                    adminEmail,
                    business.Name,
                    template.Label,
                    template.Name);
            }
            catch (Exception ex)
            {
                // One admin's mailbox rejecting the message shouldn't stop the rest
                // from being notified -- Hangfire's retry would otherwise re-send to
                // admins who already received it successfully.
                _logger.LogError(
                    ex,
                    "Failed to notify {AdminEmail} that business {BusinessId} chose template {WebsiteTemplateId}.",
                    adminEmail,
                    businessId,
                    websiteTemplateId);
            }
        }
    }
}
