namespace MerchForge.api.Services.Email.Interfaces;

public interface IEmailService
{
    Task SendBusinessOwnerInvitationAsync(
        string email,
        string invitationLink,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    Task SendWebsiteTemplateRequestSubmittedNotificationAsync(
        string adminEmail,
        string businessName,
        string ownerFullName,
        string templateLabel,
        string domainName,
        string customizationNotes,
        string dashboardLink,
        CancellationToken cancellationToken = default);

    Task SendWebsiteBuildStartedNotificationAsync(
        string ownerEmail,
        string businessName,
        string templateLabel,
        CancellationToken cancellationToken = default);
}
