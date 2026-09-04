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
    )
    {
        if (!_settings.Enabled)
            return false;

        try
        {
            using var message = new MailMessage(_settings.From, recipient)
            {
                Subject = "Verifica tu cuenta de UCB Hold",
                Body = $"Confirma tu correo institucional desde este enlace, válido durante 24 horas:\n\n{verificationUrl}",
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
            _logger.LogError(exception, "No se pudo enviar el correo de verificación");
            return false;
        }
    }
}
