namespace IMT_Reservas.Server.Application.Abstraction;

public interface IEmailSender
{
    bool IsEnabled { get; }

    Task<bool> Send(EmailMessage message, CancellationToken cancellationToken);
}
