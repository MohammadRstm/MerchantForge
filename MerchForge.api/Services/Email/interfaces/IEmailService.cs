namespace MerchForge.api.Services.Email.Interfaces;

public interface IEmailService
{
    Task SendBusinessOwnerInvitationAsync(
        string email,
        string invitationLink,
        DateTime expiresAt,
        CancellationToken cancellationToken = default);

    Task SendWebsiteTemplateChosenNotificationAsync(
        string adminEmail,
        string businessName,
        string templateLabel,
        string templateName,
        CancellationToken cancellationToken = default);
}