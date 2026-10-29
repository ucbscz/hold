using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using IMT_Reservas.Server.Modules.Usuarios.Api;
using IMT_Reservas.Server.Modules.Usuarios.Application;
using IMT_Reservas.Server.Shared.Email;
using IMT_Reservas.Server.Shared.Web;

namespace IMT_Reservas.Tests.Unit.Shared;

[TestFixture]
internal sealed class ExternalServiceRegistrationTests
{
    private static readonly JwtSettings Jwt = new()
    {
        Key = "test_key_at_least_32_chars_long!!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
    };

    private static IConfiguration Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static ServiceProvider BuildProvider(IServiceCollection services)
    {
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        return services.BuildServiceProvider();
    }

    [Test]
    public void AddEmailDelivery_Disabled_UsesDisabledSender()
    {
        var services = new ServiceCollection();

        services.AddEmailDelivery(Build(new() { ["Email:Enabled"] = "false" }));

        using var provider = BuildProvider(services);
        provider.GetRequiredService<IEmailSender>().Should().BeOfType<DisabledEmailSender>();
        provider.GetRequiredService<IEmailSender>().IsEnabled.Should().BeFalse();
    }

    [Test]
    public void AddEmailDelivery_SmtpProvider_UsesSmtpSender()
    {
        var services = new ServiceCollection();

        services.AddEmailDelivery(
            Build(
                new()
                {
                    ["Email:Enabled"] = "true",
                    ["Email:Provider"] = nameof(EmailProvider.Smtp),
                    ["Email:From"] = "sender@example.org",
                    ["Email:Host"] = "smtp.example.org",
                }
            )
        );

        using var provider = BuildProvider(services);
        provider.GetRequiredService<IEmailSender>().Should().BeOfType<SmtpEmailSender>();
    }

    [TestCase("", "smtp.example.org")]
    [TestCase("sender@example.org", "")]
    public void AddEmailDelivery_EnabledWithoutRequiredValues_Throws(string from, string host)
    {
        var action = () =>
            new ServiceCollection().AddEmailDelivery(
                Build(
                    new()
                    {
                        ["Email:Enabled"] = "true",
                        ["Email:From"] = from,
                        ["Email:Host"] = host,
                    }
                )
            );

        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public async Task EmailDeliveryService_ComposesMessageFromConfiguredSenderName()
    {
        var sender = new RecordingEmailSender();
        var service = new EmailDeliveryService(
            sender,
            Options.Create(new EmailSettings { SenderName = "Plataforma" })
        );

        await service.SendPasswordReset(
            "user@example.org",
            "123456",
            TimeSpan.FromMinutes(30),
            CancellationToken.None
        );

        sender.Message!.Recipient.Should().Be("user@example.org");
        sender.Message.Subject.Should().Contain("Plataforma");
        sender.Message.Body.Should().Contain("123456").And.Contain("30");
    }

    [TestCase("ucb.edu.bo")]
    [TestCase("@ucb.edu.bo")]
    public void InstitutionalEmailPolicy_UsesConfiguredDomain(string domain)
    {
        var policy = new InstitutionalEmailPolicy(
            Options.Create(new AuthenticationSettings { InstitutionalDomain = domain })
        );

        policy.Accepts("ana@UCB.edu.bo").Should().BeTrue();
        policy.Accepts("ana@gmail.com").Should().BeFalse();
        policy.Accepts("ana@fakeucb.edu.bo").Should().BeFalse();
        policy.Suffix.Should().Be("@ucb.edu.bo");
    }

    [Test]
    public void AddPlatformAuthentication_PartialCredentials_Throws()
    {
        var action = () =>
            new ServiceCollection().AddPlatformAuthentication(
                Build(
                    new()
                    {
                        ["Authentication:InstitutionalDomain"] = "ucb.edu.bo",
                        ["Authentication:Google:ClientId"] = "client",
                    }
                ),
                Jwt
            );

        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void AddPlatformAuthentication_WithoutInstitutionalDomain_Throws()
    {
        var action = () => new ServiceCollection().AddPlatformAuthentication(Build(new()), Jwt);

        action.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void AddPlatformAuthentication_ConfiguredProvider_ExposesScheme()
    {
        var services = new ServiceCollection();

        services.AddPlatformAuthentication(
            Build(
                new()
                {
                    ["Authentication:Provider"] = nameof(ExternalIdentityProvider.Google),
                    ["Authentication:InstitutionalDomain"] = "ucb.edu.bo",
                    ["Authentication:Google:ClientId"] = "client",
                    ["Authentication:Google:ClientSecret"] = "secret",
                }
            ),
            Jwt
        );

        using var provider = BuildProvider(services);
        var scheme = provider.GetRequiredService<ExternalIdentityScheme>();
        scheme.IsConfigured.Should().BeTrue();
        scheme.Name.Should().Be(nameof(ExternalIdentityProvider.Google));
        ApiPaths.ExternalCallback(scheme.Name).Should().Be("/api/auth/google/callback");
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public EmailMessage? Message { get; private set; }

        public bool IsEnabled => true;

        public Task<bool> Send(EmailMessage message, CancellationToken cancellationToken)
        {
            Message = message;
            return Task.FromResult(true);
        }
    }
}
