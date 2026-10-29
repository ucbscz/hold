using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using IMT_Reservas.Tests.Helpers;
using IMT_Reservas.Server.Shared.Security;

namespace IMT_Reservas.Tests.Unit.Shared;

[TestFixture]
internal sealed class DataProtectionConfigurationTests
{
    private static readonly Dictionary<string, string?> IdentitySettings = new()
    {
        ["DataProtection:ApplicationName"] = TestBranding.DataProtection.ApplicationName,
        ["DataProtection:PayloadMarker"] = TestBranding.DataProtection.PayloadMarker,
        ["DataProtection:TextMarker"] = TestBranding.DataProtection.TextMarker,
    };

    [Test]
    public void Configure_ProductionPersistentKeysWithoutCertificate_Throws()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>(IdentitySettings)
                {
                    ["DataProtection:KeysPath"] = Path.GetTempPath(),
                }
            )
            .Build();
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns(Environments.Production);

        var action = () =>
            DataProtectionConfiguration.Configure(services, configuration, environment.Object);

        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Configure_WithoutPersistentPath_UsesFrameworkDefaults()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(IdentitySettings).Build();
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns(Environments.Production);

        var action = () =>
            DataProtectionConfiguration.Configure(services, configuration, environment.Object);

        action.Should().NotThrow();
    }

    [Test]
    public void Configure_WithoutIdentity_Throws()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns(Environments.Production);

        var action = () =>
            DataProtectionConfiguration.Configure(
                new ServiceCollection(),
                new ConfigurationBuilder().Build(),
                environment.Object
            );

        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Configure_WithCertificate_EncryptsPersistedKeyMaterial()
    {
        var directory = Directory.CreateTempSubdirectory("data-protection-");
        try
        {
            const string password = "test-certificate-password";
            var certificatePath = Path.Combine(directory.FullName, "protection.pfx");
            var passwordPath = Path.Combine(directory.FullName, "password.txt");
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=UCB Hold Test",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1
            );
            using var certificate = request.CreateSelfSigned(
                DateTimeOffset.UtcNow.AddMinutes(-1),
                DateTimeOffset.UtcNow.AddDays(1)
            );
            File.WriteAllBytes(certificatePath, certificate.Export(X509ContentType.Pfx, password));
            File.WriteAllText(passwordPath, password);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>(IdentitySettings)
                    {
                        ["DataProtection:KeysPath"] = directory.FullName,
                        ["DataProtection:CertificatePath"] = certificatePath,
                        ["DataProtection:CertificatePasswordFile"] = passwordPath,
                    }
                )
                .Build();
            var environment = new Mock<IWebHostEnvironment>();
            environment.SetupGet(item => item.EnvironmentName).Returns(Environments.Production);
            var services = new ServiceCollection();

            DataProtectionConfiguration.Configure(services, configuration, environment.Object);
            using var provider = services.BuildServiceProvider();
            provider.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test")
                .Protect("sensitive-value");

            var keyXml = File.ReadAllText(Directory.GetFiles(directory.FullName, "key-*.xml").Single());
            keyXml.Should().Contain("encryptedSecret");
            keyXml.Should().NotContain("sensitive-value");
        }
        finally
        {
            directory.Delete(true);
        }
    }
}
