using Hangfire;
using MerchForge.api.Data;
using MerchForge.api.Services.Email.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Jobs.Email;

/// <summary>
/// Notifies the owner who submitted a website template request that a SuperAdmin has
/// started building it.
/// </summary>
public class NotifyOwnerOfWebsiteBuildStartedJob
{
    private readonly MerchForgeDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotifyOwnerOfWebsiteBuildStartedJob> _logger;

    public NotifyOwnerOfWebsiteBuildStartedJob(
        MerchForgeDbContext db,
        IEmailService emailService,
        ILogger<NotifyOwnerOfWebsiteBuildStartedJob> logger)
    {
        _db = db;
        _emailService = emailService;
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
                TemplateLabel = r.WebsiteTemplate.Label,
                RequestedByEmail = r.RequestedByUserId,
            })
            .FirstOrDefaultAsync();

        if (request is null)
        {
            _logger.LogWarning(
                "WebsiteTemplateRequest {WebsiteTemplateRequestId} was not found. Build-started notification skipped.",
                websiteTemplateRequestId);
            return;
        }

        var ownerEmail = await _db.Users
            .Where(u => u.Id == request.RequestedByEmail)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();

        if (ownerEmail is null)
        {
            _logger.LogWarning(
                "Requesting user for WebsiteTemplateRequest {WebsiteTemplateRequestId} was not found. Build-started notification skipped.",
                websiteTemplateRequestId);
            return;
        }

        try
        {
            await _emailService.SendWebsiteBuildStartedNotificationAsync(
                ownerEmail,
                request.BusinessName,
                request.TemplateLabel);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to notify {OwnerEmail} that the build started for website template request {WebsiteTemplateRequestId}.",
                ownerEmail,
                websiteTemplateRequestId);
        }
    }
}
