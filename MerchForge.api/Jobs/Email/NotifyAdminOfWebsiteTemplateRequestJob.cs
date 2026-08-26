using Hangfire;
using MerchForge.api.Data;
using MerchForge.api.Enums;
using MerchForge.api.Services.Email.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MerchForge.api.Jobs.Email;

/// <summary>
/// Notifies every SuperAdmin when a business owner submits a website template
/// request, so they know a new build needs review.
/// </summary>
public class NotifyAdminOfWebsiteTemplateRequestJob
{
    private readonly MerchForgeDbContext _db;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotifyAdminOfWebsiteTemplateRequestJob> _logger;

    public NotifyAdminOfWebsiteTemplateRequestJob(
        MerchForgeDbContext db,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<NotifyAdminOfWebsiteTemplateRequestJob> logger)
    {
        _db = db;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(Guid websiteTemplateRequestId)
    {
        var request = await _db.WebsiteTemplateRequests
            .Where(r => r.Id == websiteTemplateRequestId)
            .Select(r => new
            {
                BusinessName = r.Business.Name,
                OwnerFullName = r.Business.Owner.FirstName + " " + r.Business.Owner.LastName,
                TemplateLabel = r.WebsiteTemplate.Label,
                DomainName = r.Business.BusinessDomain != null ? r.Business.BusinessDomain.Name : "",
                r.CustomizationNotes,
            })
            .FirstOrDefaultAsync();

        if (request is null)
        {
            _logger.LogWarning(
                "WebsiteTemplateRequest {WebsiteTemplateRequestId} was not found. Website-template-request notification skipped.",
                websiteTemplateRequestId);
            return;
        }

        var adminEmails = await _db.Users
            .Where(u => _db.SystemRoles.Any(r => r.Id == u.SystemRoleId && r.Role == SystemRole.SuperAdmin))
            .Select(u => u.Email)
            .ToListAsync();

        var dashboardLink = $"{_configuration["Frontend:BaseUrl"]}/dashboard#website-requests";

        foreach (var adminEmail in adminEmails)
        {
            try
            {
                await _emailService.SendWebsiteTemplateRequestSubmittedNotificationAsync(
                    adminEmail,
                    request.BusinessName,
                    request.OwnerFullName,
                    request.TemplateLabel,
                    request.DomainName,
                    request.CustomizationNotes,
                    dashboardLink);
            }
            catch (Exception ex)
            {
                // One admin's mailbox rejecting the message shouldn't stop the rest
                // from being notified -- Hangfire's retry would otherwise re-send to
                // admins who already received it successfully.
                _logger.LogError(
                    ex,
                    "Failed to notify {AdminEmail} of website template request {WebsiteTemplateRequestId}.",
                    adminEmail,
                    websiteTemplateRequestId);
            }
        }
    }
}
