using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Shared.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailSettings _email;
    private readonly SmtpSettings _smtp;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(
        IOptions<EmailSettings> email,
        IOptions<SmtpSettings> smtp,
        ILogger<SmtpEmailSender> logger
    )
    {
        _email = email.Value;
        _smtp = smtp.Value;
        _logger = logger;
    }

    public bool IsEnabled => true;

    public async Task<bool> Send(EmailMessage message, CancellationToken cancellationToken)
    {
        try
        {
            using var mail = new MailMessage(
                new MailAddress(_email.From, _email.SenderName),
                new MailAddress(message.Recipient)
            )
            {
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = false,
            };
            using var client = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                EnableSsl = _smtp.EnableSsl,
                Credentials = new NetworkCredential(_smtp.Username, _smtp.Password),
                Timeout = _smtp.TimeoutMilliseconds,
            };
            await client.SendMailAsync(mail, cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            _logger.EmailDeliveryFailed(exception);
            return false;
        }
    }
}
