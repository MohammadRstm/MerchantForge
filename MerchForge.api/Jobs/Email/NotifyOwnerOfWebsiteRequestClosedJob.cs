using Hangfire;
using MerchForge.api.Data;
using MerchForge.api.Services.Email.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MerchForge.api.Jobs.Email;

/// <summary>
/// Notifies the owner who submitted a website template request that a SuperAdmin has
/// closed it, confirming the site is live and giving them the final URL.
/// </summary>
public class NotifyOwnerOfWebsiteRequestClosedJob
{
    private readonly MerchForgeDbContext _db;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotifyOwnerOfWebsiteRequestClosedJob> _logger;

    public NotifyOwnerOfWebsiteRequestClosedJob(
        MerchForgeDbContext db,
        IEmailService emailService,
        ILogger<NotifyOwnerOfWebsiteRequestClosedJob> logger)
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
                r.FinalWebsiteUrl,
                r.RequestedByUserId,
            })
            .FirstOrDefaultAsync();

        if (request is null || request.FinalWebsiteUrl is null)
        {
            _logger.LogWarning(
                "WebsiteTemplateRequest {WebsiteTemplateRequestId} was not found or has no final URL. Request-closed notification skipped.",
                websiteTemplateRequestId);
            return;
        }

        var ownerEmail = await _db.Users
            .Where(u => u.Id == request.RequestedByUserId)
            .Select(u => u.Email)
            .FirstOrDefaultAsync();

        if (ownerEmail is null)
        {
            _logger.LogWarning(
                "Requesting user for WebsiteTemplateRequest {WebsiteTemplateRequestId} was not found. Request-closed notification skipped.",
                websiteTemplateRequestId);
            return;
        }

        try
        {
            await _emailService.SendWebsiteRequestClosedNotificationAsync(
                ownerEmail,
                request.BusinessName,
                request.FinalWebsiteUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to notify {OwnerEmail} that website template request {WebsiteTemplateRequestId} was closed.",
                ownerEmail,
                websiteTemplateRequestId);
        }
    }
}
