using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Usuario;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using IMT_Reservas.Server.Application.Features.Jwt;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
internal class JwtServiceTests
{
    private JwtService _jwtService = null!;

    private static readonly JwtSettings TestSettings = new()
    {
        Key = "test_key_at_least_32_chars_long!!",
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        ExpiresInMinutes = 60,
        RefreshTokenExpiryDays = 7
    };

    private static UsuarioDto BuildUsuarioDto() => new()
    {
        Carnet = "U001",
        Email = "u001@ucb.edu.bo",
        Nombre = "Test",
        Rol = "estudiante"
    };

    [SetUp]
    public void SetUp()
    {
        _jwtService = new JwtService(Options.Create(TestSettings));
    }

    [Test]
    public void GenerateAccessToken_ReturnsNonEmptyString()
    {
        var token = _jwtService.GenerateAccessToken(BuildUsuarioDto());

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void GenerateAccessToken_ContainsSubClaim()
    {
        var token = _jwtService.GenerateAccessToken(BuildUsuarioDto());

        var parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsedToken.Subject.Should().Be("U001");
    }

    [Test]
    public void GenerateAccessToken_ContainsRoleClaim()
    {
        var token = _jwtService.GenerateAccessToken(BuildUsuarioDto());

        var parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsedToken.Claims.Should().Contain(c => c.Type == "role" && c.Value == "estudiante");
    }

    [Test]
    public void GenerateAccessToken_DoesNotContainPersonalProfileData()
    {
        var token = _jwtService.GenerateAccessToken(BuildUsuarioDto());

        var parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsedToken.Claims.Should().NotContain(c => c.Type == JwtRegisteredClaimNames.Email);
        parsedToken.Claims.Should().NotContain(c => c.Type == "nombre");
        token.Should().NotContain("u001@ucb.edu.bo");
        token.Should().NotContain("Test");
    }

    [Test]
    public void GenerateAccessToken_HasFutureExpiry()
    {
        var beforeGeneration = DateTime.UtcNow;

        var token = _jwtService.GenerateAccessToken(BuildUsuarioDto());

        var parsedToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
        parsedToken.ValidTo.Should().BeAfter(beforeGeneration);
    }

    [Test]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var refreshToken = JwtService.GenerateRefreshToken();

        refreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void GenerateRefreshToken_IsDifferentEachCall()
    {
        var firstToken = JwtService.GenerateRefreshToken();
        var secondToken = JwtService.GenerateRefreshToken();

        firstToken.Should().NotBe(secondToken);
    }
}
