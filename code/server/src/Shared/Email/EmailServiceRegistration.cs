using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.Usuario;

namespace IMT_Reservas.Server.Infrastructure.Email;

public static class EmailServiceRegistration
{
    private const int MinimumPort = 1;
    private const int MaximumPort = 65535;

    public static IServiceCollection AddEmailDelivery(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var section = configuration.GetSection(EmailSettings.SectionName);
        var settings = section.Get<EmailSettings>() ?? new EmailSettings();
        services.Configure<EmailSettings>(section);
        services.AddScoped<EmailDeliveryService>();

        if (!settings.Enabled)
            return services.AddSingleton<IEmailSender, DisabledEmailSender>();

        Require(settings.From, nameof(EmailSettings.From));
        return settings.Provider switch
        {
            EmailProvider.Smtp => services.AddSmtp(section),
            _ => throw new InvalidOperationException(
                $"{EmailSettings.SectionName}:{nameof(EmailSettings.Provider)} {settings.Provider} is not supported."
            ),
        };
    }

    private static IServiceCollection AddSmtp(
        this IServiceCollection services,
        IConfigurationSection section
    )
    {
        var smtp = section.Get<SmtpSettings>() ?? new SmtpSettings();
        Require(smtp.Host, nameof(SmtpSettings.Host));
        if (smtp.Port is < MinimumPort or > MaximumPort)
            throw new InvalidOperationException(
                $"{EmailSettings.SectionName}:{nameof(SmtpSettings.Port)} must be between {MinimumPort} and {MaximumPort}."
            );

        services.Configure<SmtpSettings>(section);
        return services.AddSingleton<IEmailSender, SmtpEmailSender>();
    }

    private static void Require(string value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"{EmailSettings.SectionName}:{key} is required when email is enabled."
            );
    }
}
