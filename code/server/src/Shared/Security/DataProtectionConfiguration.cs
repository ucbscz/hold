using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;

namespace UCB_Hold.Server.Shared.Security;

public static class DataProtectionConfiguration
{
    public static void Configure(
        IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        var section = configuration.GetSection(DataProtectionSettings.SectionName);
        var settings = section.Get<DataProtectionSettings>() ?? new DataProtectionSettings();
        RequireIdentity(settings);
        services.Configure<DataProtectionSettings>(section);

        var builder = services.AddDataProtection().SetApplicationName(settings.ApplicationName);
        var keysPath = settings.KeysPath;
        if (string.IsNullOrWhiteSpace(keysPath))
            return;

        builder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

        var certificatePath = settings.CertificatePath;
        var passwordFile = settings.CertificatePasswordFile;
        if (string.IsNullOrWhiteSpace(certificatePath) || string.IsNullOrWhiteSpace(passwordFile))
        {
            if (environment.IsProduction())
                throw new InvalidOperationException(
                    "A Data Protection certificate and password file are required in production."
                );
            return;
        }

        if (!File.Exists(certificatePath) || !File.Exists(passwordFile))
            throw new InvalidOperationException("Data Protection secret files were not found.");

        var password = File.ReadAllText(passwordFile).Trim();
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("The Data Protection certificate password is empty.");

        var certificate = new X509Certificate2(
            certificatePath,
            password,
            X509KeyStorageFlags.EphemeralKeySet
        );
        if (!certificate.HasPrivateKey)
            throw new InvalidOperationException("The Data Protection certificate has no private key.");

        builder.ProtectKeysWithCertificate(certificate);
    }

    private static void RequireIdentity(DataProtectionSettings settings)
    {
        if (
            string.IsNullOrWhiteSpace(settings.ApplicationName)
            || string.IsNullOrWhiteSpace(settings.PayloadMarker)
            || string.IsNullOrWhiteSpace(settings.TextMarker)
        )
            throw new InvalidOperationException(
                $"{DataProtectionSettings.SectionName}:{nameof(DataProtectionSettings.ApplicationName)}, {nameof(DataProtectionSettings.PayloadMarker)} and {nameof(DataProtectionSettings.TextMarker)} are required."
            );
    }
}
