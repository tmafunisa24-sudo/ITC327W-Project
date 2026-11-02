namespace ServiceHub_IT.Services;

public interface IEmailSender
{
    bool IsConfigured { get; }

    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}
