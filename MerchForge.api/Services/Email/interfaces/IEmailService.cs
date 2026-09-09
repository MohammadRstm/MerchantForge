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

    /// <summary>
    /// Forwards a public contact-form enquiry to an administrator.
    ///
    /// senderEmail becomes the message's Reply-To, never its From. Putting a
    /// stranger's address in From would be sending mail as them from our relay,
    /// which fails SPF and DKIM and gets the message rejected or filed as spam -
    /// Reply-To gets the same "just hit reply" behaviour with none of that.
    /// </summary>
    Task SendContactEnquiryNotificationAsync(
        string adminEmail,
        string senderName,
        string senderEmail,
        string subject,
        string message,
        CancellationToken cancellationToken = default);
}
