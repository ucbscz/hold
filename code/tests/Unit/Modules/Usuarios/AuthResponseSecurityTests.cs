using System.Text.Json;
using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Usuario;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
internal sealed class AuthResponseSecurityTests
{
    [Test]
    public void LoginDto_Serialization_DoesNotExposeTokens()
    {
        var dto = new LoginDto
        {
            AccessToken = "access-secret",
            RefreshToken = "refresh-secret",
            Usuario = new UsuarioDto { Carnet = "U001" },
        };

        var json = JsonSerializer.Serialize(dto);

        json.Should().NotContain("access-secret");
        json.Should().NotContain("refresh-secret");
        json.Should().Contain("U001");
    }
}
