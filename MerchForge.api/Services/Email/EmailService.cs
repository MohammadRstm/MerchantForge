using MailKit.Net.Smtp;
using MailKit.Security;
using MerchForge.api.Configurations;
using MerchForge.api.Exceptions.Email;
using MerchForge.api.Services.Email.Interfaces;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MerchForge.api.Services.Email;

public class EmailService : IEmailService
{
    private readonly EmailOptions _options;

    public EmailService(IOptions<EmailOptions> options)
    {
        _options = options.Value;
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
        catch (Exception)
        {
            throw new EmailDeliveryException();
        }
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