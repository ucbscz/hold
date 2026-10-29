namespace UCB_Hold.Server.Shared.Email;

public interface IEmailSender
{
    bool IsEnabled { get; }

    Task<bool> Send(EmailMessage message, CancellationToken cancellationToken);
}
