using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Jwt;
using IMT_Reservas.Server.Application.Features.Usuario;
using IMT_Reservas.Server.Presentation.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
internal sealed class AuthCookieServiceTests
{
    [Test]
    public void Write_InProduction_UsesProtectedStrictCookies()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns(Environments.Production);
        var service = new AuthCookieService(
            Options.Create(new JwtSettings { ExpiresInMinutes = 15, RefreshTokenExpiryDays = 7 }),
            environment.Object
        );
        var context = new DefaultHttpContext();

        service.Write(
            context.Response.Cookies,
            new LoginDto { AccessToken = "access", RefreshToken = "refresh" }
        );

        var headers = context.Response.Headers.SetCookie.ToArray();
        headers.Should().HaveCount(2);
        headers.Should().OnlyContain(header =>
            header!.Contains("httponly", StringComparison.OrdinalIgnoreCase)
            && header.Contains("secure", StringComparison.OrdinalIgnoreCase)
            && header.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase)
        );
        headers.Should().Contain(header => header!.StartsWith($"{AuthCookieService.AccessTokenName}=", StringComparison.Ordinal));
        headers.Should().Contain(header => header!.StartsWith($"{AuthCookieService.RefreshTokenName}=", StringComparison.Ordinal));
    }

    [Test]
    public void Write_InDevelopment_AllowsHttpLocalhost()
    {
        var environment = new Mock<IWebHostEnvironment>();
        environment.SetupGet(item => item.EnvironmentName).Returns(Environments.Development);
        var service = new AuthCookieService(
            Options.Create(new JwtSettings { ExpiresInMinutes = 15, RefreshTokenExpiryDays = 7 }),
            environment.Object
        );
        var context = new DefaultHttpContext();

        service.Write(
            context.Response.Cookies,
            new LoginDto { AccessToken = "access", RefreshToken = "refresh" }
        );

        context.Response.Headers.SetCookie.Should().OnlyContain(header =>
            !header!.Contains("secure", StringComparison.OrdinalIgnoreCase)
        );
    }
}
