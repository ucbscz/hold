using IMT_Reservas.Server.Application.Abstraction;

namespace IMT_Reservas.Server.Infrastructure.Email;

public sealed class DisabledEmailSender : IEmailSender
{
    public bool IsEnabled => false;

    public Task<bool> Send(EmailMessage message, CancellationToken cancellationToken) =>
        Task.FromResult(false);
}
