namespace MerchForge.api.Services.Email.Interfaces;

public interface IEmailService
{
    Task SendBusinessOwnerInvitationAsync(
        string email,
        string invitationLink,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    Task SendBusinessMemberInvitationAsync(
        string email,
        string invitationLink,
        string businessName,
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

    Task SendWebsiteRequestClosedNotificationAsync(
        string ownerEmail,
        string businessName,
        string finalWebsiteUrl,
        CancellationToken cancellationToken = default);

    Task SendTakeWebsiteDownNotificationAsync(
        string adminEmail,
        string businessName,
        string websiteUrl,
        CancellationToken cancellationToken = default);
}
