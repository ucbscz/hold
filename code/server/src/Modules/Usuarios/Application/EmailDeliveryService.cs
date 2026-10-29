using System.Globalization;
using Microsoft.Extensions.Options;
using UCB_Hold.Server.Shared.Email;

namespace UCB_Hold.Server.Modules.Usuarios.Application;

public sealed class EmailDeliveryService
{
    private readonly IEmailSender _sender;
    private readonly string _senderName;

    public EmailDeliveryService(IEmailSender sender, IOptions<EmailSettings> settings)
    {
        _sender = sender;
        _senderName = settings.Value.SenderName;
    }

    public bool IsEnabled => _sender.IsEnabled;

    public Task<bool> SendVerification(
        string recipient,
        string verificationUrl,
        TimeSpan lifetime,
        CancellationToken cancellationToken
    ) =>
        _sender.Send(
            new EmailMessage(
                recipient,
                Format(EmailTemplates.VerificationSubject, _senderName),
                Format(EmailTemplates.VerificationBody, lifetime.TotalHours, verificationUrl)
            ),
            cancellationToken
        );

    public Task<bool> SendPasswordReset(
        string recipient,
        string resetCode,
        TimeSpan lifetime,
        CancellationToken cancellationToken
    ) =>
        _sender.Send(
            new EmailMessage(
                recipient,
                Format(EmailTemplates.PasswordResetSubject, _senderName),
                Format(EmailTemplates.PasswordResetBody, resetCode, lifetime.TotalMinutes)
            ),
            cancellationToken
        );

    private static string Format(string template, params object[] values) =>
        string.Format(CultureInfo.InvariantCulture, template, values);
}
