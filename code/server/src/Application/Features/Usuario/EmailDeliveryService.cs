using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace IMT_Reservas.Server.Application.Features.Usuario;

public sealed class EmailDeliveryService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailDeliveryService> _logger;

    public EmailDeliveryService(
        IOptions<EmailSettings> settings,
        ILogger<EmailDeliveryService> logger
    )
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendVerification(
        string recipient,
        string verificationUrl,
        CancellationToken cancellationToken
    ) => await Send(
        recipient,
        "Verifica tu cuenta de UCB Hold",
        $"Confirma tu correo institucional desde este enlace, válido durante 24 horas:\n\n{verificationUrl}",
        cancellationToken
    );

    public async Task<bool> SendPasswordReset(
        string recipient,
        string resetUrl,
        CancellationToken cancellationToken
    ) => await Send(
        recipient,
        "Restablece tu contraseña de UCB Hold",
        $"Solicitaste restablecer tu contraseña. Usa este enlace una sola vez, válido durante 30 minutos:\n\n{resetUrl}\n\nSi no realizaste esta solicitud, puedes ignorar este correo.",
        cancellationToken
    );

    private async Task<bool> Send(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken
    )
    {
        if (!_settings.Enabled)
            return false;

        try
        {
            using var message = new MailMessage(_settings.From, recipient)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };
            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                Timeout = 15000,
            };
            await client.SendMailAsync(message).WaitAsync(cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "No se pudo enviar el correo");
            return false;
        }
    }
}
