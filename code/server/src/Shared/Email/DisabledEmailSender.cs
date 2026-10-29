namespace UCB_Hold.Server.Shared.Email;

public sealed class DisabledEmailSender : IEmailSender
{
    public bool IsEnabled => false;

    public Task<bool> Send(EmailMessage message, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
