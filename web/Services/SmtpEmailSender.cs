using System.Net;
using System.Net.Mail;

namespace ServiceHub_IT.Services;

/// <summary>Optional SMTP delivery. Email is enabled only when host and sender are configured.</summary>
public sealed class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Email:SmtpHost"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Email:FromAddress"]);

    public async Task SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || string.IsNullOrWhiteSpace(recipient))
        {
            return;
        }

        var host = _configuration["Email:SmtpHost"]!;
        var port = int.TryParse(_configuration["Email:Port"], out var configuredPort) ? configuredPort : 587;
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];
        var enableSsl = !bool.TryParse(_configuration["Email:EnableSsl"], out var ssl) || ssl;

        using var message = new MailMessage(_configuration["Email:FromAddress"]!, recipient, subject, body);
#pragma warning disable SYSLIB0014 // SmtpClient is retained for optional, dependency-free SMTP configuration.
        using var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl,
            Credentials = string.IsNullOrWhiteSpace(username)
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(username, password),
        };

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
#pragma warning restore SYSLIB0014
    }
}
