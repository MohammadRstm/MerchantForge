using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.Email;
using MerchForge.api.Jobs.Email;
using MerchForge.api.Services.Email.Interfaces;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MerchForge.api.Services.Email;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailOptions> options, ILogger<EmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendBusinessOwnerInvitationAsync(
        string email,
        string invitationLink,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _options.FromName,
                    _options.FromEmail));

            message.To.Add(
                MailboxAddress.Parse(email));

            message.Subject = "You're invited to MerchForge";

            var body = BuildBusinessOwnerInvitationEmail(
                invitationLink,
                expiresAt);

            message.Body = new BodyBuilder
            {
                HtmlBody = body
            }.ToMessageBody();

            using var smtpClient = new SmtpClient();

            await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await smtpClient.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);

            await smtpClient.SendAsync(
                message,
                cancellationToken);

            await smtpClient.DisconnectAsync(
                true,
                cancellationToken);
        }
        catch(OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {Email}. SMTP Host: {Host}, Port: {Port}",
                email,
                _options.Host,
                _options.Port);
            throw new EmailDeliveryException();
        }
    }

    public async Task SendBusinessMemberInvitationAsync(
        string email,
        string invitationLink,
        string businessName,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _options.FromName,
                    _options.FromEmail));

            message.To.Add(
                MailboxAddress.Parse(email));

            message.Subject = $"You're invited to join {businessName} on MerchForge";

            var body = BuildBusinessMemberInvitationEmail(
                businessName,
                invitationLink,
                expiresAt);

            message.Body = new BodyBuilder
            {
                HtmlBody = body
            }.ToMessageBody();

            using var smtpClient = new SmtpClient();

            await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await smtpClient.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);

            await smtpClient.SendAsync(
                message,
                cancellationToken);

            await smtpClient.DisconnectAsync(
                true,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email to {Email}. SMTP Host: {Host}, Port: {Port}",
                email,
                _options.Host,
                _options.Port);
            throw new EmailDeliveryException();
        }
    }

    public async Task SendWebsiteTemplateRequestSubmittedNotificationAsync(
        string adminEmail,
        string businessName,
        string ownerFullName,
        string templateLabel,
        string domainName,
        string customizationNotes,
        string dashboardLink,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _options.FromName,
                    _options.FromEmail));

            message.To.Add(
                MailboxAddress.Parse(adminEmail));

            message.Subject = $"New website request from {businessName}";

            var body = BuildWebsiteTemplateRequestSubmittedEmail(
                businessName, ownerFullName, templateLabel, domainName, customizationNotes, dashboardLink);

            message.Body = new BodyBuilder
            {
                HtmlBody = body
            }.ToMessageBody();

            using var smtpClient = new SmtpClient();

            await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await smtpClient.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);

            await smtpClient.SendAsync(
                message,
                cancellationToken);

            await smtpClient.DisconnectAsync(
                true,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send website-template-request notification to {Email}. SMTP Host: {Host}, Port: {Port}",
                adminEmail,
                _options.Host,
                _options.Port);
            throw new EmailDeliveryException();
        }
    }

    public async Task SendWebsiteBuildStartedNotificationAsync(
        string ownerEmail,
        string businessName,
        string templateLabel,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _options.FromName,
                    _options.FromEmail));

            message.To.Add(
                MailboxAddress.Parse(ownerEmail));

            message.Subject = "Your MerchForge website build has started";

            var body = BuildWebsiteBuildStartedEmail(businessName, templateLabel);

            message.Body = new BodyBuilder
            {
                HtmlBody = body
            }.ToMessageBody();

            using var smtpClient = new SmtpClient();

            await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await smtpClient.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);

            await smtpClient.SendAsync(
                message,
                cancellationToken);

            await smtpClient.DisconnectAsync(
                true,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send website-build-started notification to {Email}. SMTP Host: {Host}, Port: {Port}",
                ownerEmail,
                _options.Host,
                _options.Port);
            throw new EmailDeliveryException();
        }
    }

    public async Task SendWebsiteRequestClosedNotificationAsync(
        string ownerEmail,
        string businessName,
        string finalWebsiteUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _options.FromName,
                    _options.FromEmail));

            message.To.Add(
                MailboxAddress.Parse(ownerEmail));

            message.Subject = "Your MerchForge website is live";

            var body = BuildWebsiteRequestClosedEmail(businessName, finalWebsiteUrl);

            message.Body = new BodyBuilder
            {
                HtmlBody = body
            }.ToMessageBody();

            using var smtpClient = new SmtpClient();

            await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await smtpClient.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);

            await smtpClient.SendAsync(
                message,
                cancellationToken);

            await smtpClient.DisconnectAsync(
                true,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send website-request-closed notification to {Email}. SMTP Host: {Host}, Port: {Port}",
                ownerEmail,
                _options.Host,
                _options.Port);
            throw new EmailDeliveryException();
        }
    }

    public async Task SendTakeWebsiteDownNotificationAsync(
        string adminEmail,
        string businessName,
        string websiteUrl,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _options.FromName,
                    _options.FromEmail));

            message.To.Add(
                MailboxAddress.Parse(adminEmail));

            message.Subject = $"Take down {businessName}'s website — subscription ended";

            var body = BuildTakeWebsiteDownEmail(businessName, websiteUrl);

            message.Body = new BodyBuilder
            {
                HtmlBody = body
            }.ToMessageBody();

            using var smtpClient = new SmtpClient();

            await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await smtpClient.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);

            await smtpClient.SendAsync(
                message,
                cancellationToken);

            await smtpClient.DisconnectAsync(
                true,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send take-website-down notification to {Email}. SMTP Host: {Host}, Port: {Port}",
                adminEmail,
                _options.Host,
                _options.Port);
            throw new EmailDeliveryException();
        }
    }

    public async Task SendContactEnquiryNotificationAsync(
        string adminEmail,
        string senderName,
        string senderEmail,
        string subject,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mail = new MimeMessage();

            // From stays ours. The sender's address goes in Reply-To instead:
            // putting it in From would be sending mail as them through our relay,
            // which fails SPF and DKIM and gets the message rejected or spam-filed.
            mail.From.Add(
                new MailboxAddress(
                    _options.FromName,
                    _options.FromEmail));

            mail.To.Add(
                MailboxAddress.Parse(adminEmail));

            // Parsed rather than interpolated, so a name containing a comma or an
            // angle bracket cannot inject a second recipient into the header.
            mail.ReplyTo.Add(
                new MailboxAddress(senderName, senderEmail));

            // The subject is stranger-supplied, so it is prefixed rather than used
            // as-is - an admin should never have to guess whether a subject line
            // in their inbox came from MerchForge or from someone writing to it.
            // MimeKit encodes header values, so a newline here cannot split headers.
            mail.Subject = $"[Contact form] {subject}";

            mail.Body = new BodyBuilder
            {
                HtmlBody = BuildContactEnquiryEmail(senderName, senderEmail, subject, message),
                // A plain-text alternative, and not only for old clients: it is
                // the copy that cannot render anything a sender put in the body.
                TextBody = $"From: {senderName} <{senderEmail}>\nSubject: {subject}\n\n{message}",
            }.ToMessageBody();

            using var smtpClient = new SmtpClient();

            await smtpClient.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.StartTls,
                cancellationToken);

            await smtpClient.AuthenticateAsync(
                _options.Username,
                _options.Password,
                cancellationToken);

            await smtpClient.SendAsync(
                mail,
                cancellationToken);

            await smtpClient.DisconnectAsync(
                true,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to forward a contact enquiry to {Email}. SMTP Host: {Host}, Port: {Port}",
                adminEmail,
                _options.Host,
                _options.Port);
            throw new EmailDeliveryException();
        }
    }

    private static string BuildWebsiteTemplateRequestSubmittedEmail(
        string businessName,
        string ownerFullName,
        string templateLabel,
        string domainName,
        string customizationNotes,
        string dashboardLink)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <body>
                <h2>New website request</h2>

                <p>
                    <strong>{businessName}</strong> ({ownerFullName}) has requested a
                    custom build of the <strong>{templateLabel}</strong> template
                    ({domainName}).
                </p>

                <p>
                    <strong>Customization notes:</strong><br />
                    {customizationNotes}
                </p>

                <p>
                    <a href="{dashboardLink}"
                       style="
                           display:inline-block;
                           padding:12px 20px;
                           background:#ff9b00;
                           color:white;
                           text-decoration:none;
                           border-radius:6px;
                       ">
                        Review in the admin dashboard
                    </a>
                </p>

                <p>
                    — MerchForge
                </p>
            </body>
            </html>
            """;
    }

    private static string BuildWebsiteRequestClosedEmail(
        string businessName,
        string finalWebsiteUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <body>
                <h2>Your website is live</h2>

                <p>
                    <strong>{businessName}</strong>'s custom website is built and ready.
                </p>

                <p>
                    <a href="{finalWebsiteUrl}"
                       style="
                           display:inline-block;
                           padding:12px 20px;
                           background:#ff9b00;
                           color:white;
                           text-decoration:none;
                           border-radius:6px;
                       ">
                        View your website
                    </a>
                </p>

                <p>
                    {finalWebsiteUrl}
                </p>

                <p>
                    — MerchForge
                </p>
            </body>
            </html>
            """;
    }

    private static string BuildTakeWebsiteDownEmail(
        string businessName,
        string websiteUrl)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <body>
                <h2>Subscription ended — take this website down</h2>

                <p>
                    <strong>{businessName}</strong>'s subscription has run out and was not renewed.
                    Their storefront should be taken offline.
                </p>

                <p>
                    <a href="{websiteUrl}"
                       style="
                           display:inline-block;
                           padding:12px 20px;
                           background:#ff9b00;
                           color:white;
                           text-decoration:none;
                           border-radius:6px;
                       ">
                        View website
                    </a>
                </p>

                <p>
                    {websiteUrl}
                </p>

                <p>
                    — MerchForge
                </p>
            </body>
            </html>
            """;
    }

    /// <summary>
    /// The one email body built entirely from anonymous input, and the only one
    /// here that escapes what it interpolates.
    ///
    /// Every other builder in this file drops its values straight into the
    /// markup, which is defensible where they come from an authenticated owner -
    /// a business name, a template label. This one is a public form, so
    /// unescaped text would let a stranger put working markup and links into a
    /// message that arrives from our own address and looks like we sent it.
    /// Newlines become &lt;br&gt; after escaping, never before, so the escaping
    /// cannot be walked back.
    /// </summary>
    private static string BuildContactEnquiryEmail(
        string senderName,
        string senderEmail,
        string subject,
        string message)
    {
        var name = WebUtility.HtmlEncode(senderName);
        var email = WebUtility.HtmlEncode(senderEmail);
        var safeSubject = WebUtility.HtmlEncode(subject);
        var body = WebUtility.HtmlEncode(message).Replace("\n", "<br>");

        return $"""
            <!DOCTYPE html>
            <html>
            <body>
                <h2>New enquiry from the contact form</h2>

                <p>
                    <strong>From:</strong> {name} &lt;{email}&gt;<br>
                    <strong>Subject:</strong> {safeSubject}
                </p>

                <hr>

                <p>{body}</p>

                <hr>

                <p>
                    Reply to this email to answer {name} directly.
                </p>
            </body>
            </html>
            """;
    }

    private static string BuildWebsiteBuildStartedEmail(
        string businessName,
        string templateLabel)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <body>
                <h2>Your website build has started</h2>

                <p>
                    We've started building <strong>{businessName}</strong>'s custom
                    website based on the <strong>{templateLabel}</strong> template.
                </p>

                <p>
                    We'll be in touch once it's ready.
                </p>

                <p>
                    — MerchForge
                </p>
            </body>
            </html>
            """;
    }

    private static string BuildBusinessMemberInvitationEmail(
        string businessName,
        string invitationLink,
        DateTime expiresAt)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <body>
                <h2>You're invited to join {businessName}</h2>

                <p>
                    You have been added to <strong>{businessName}</strong>'s team on
                    MerchForge.
                </p>

                <p>
                    Click the button below to set your password and finish setting up
                    your account.
                </p>

                <p>
                    <a href="{invitationLink}"
                       style="
                           display:inline-block;
                           padding:12px 20px;
                           background:#ff9b00;
                           color:white;
                           text-decoration:none;
                           border-radius:6px;
                       ">
                        Set your password
                    </a>
                </p>

                <p>
                    This invitation expires on
                    <strong>{expiresAt:MMMM dd, yyyy HH:mm} UTC</strong>.
                </p>

                <p>
                    If you did not expect this invitation,
                    you can safely ignore this email.
                </p>

                <p>
                    — MerchForge
                </p>
            </body>
            </html>
            """;
    }

    private static string BuildBusinessOwnerInvitationEmail(
        string invitationLink,
        DateTime expiresAt)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <body>
                <h2>Welcome to MerchForge</h2>

                <p>
                    You have been invited to create your
                    MerchForge business account.
                </p>

                <p>
                    Click the button below to complete your registration.
                </p>

                <p>
                    <a href="{invitationLink}"
                       style="
                           display:inline-block;
                           padding:12px 20px;
                           background:#ff9b00;
                           color:white;
                           text-decoration:none;
                           border-radius:6px;
                       ">
                        Complete Registration
                    </a>
                </p>

                <p>
                    This invitation expires on
                    <strong>{expiresAt:MMMM dd, yyyy HH:mm} UTC</strong>.
                </p>

                <p>
                    If you did not expect this invitation,
                    you can safely ignore this email.
                </p>

                <p>
                    — MerchForge
                </p>
            </body>
            </html>
            """;
    }
}