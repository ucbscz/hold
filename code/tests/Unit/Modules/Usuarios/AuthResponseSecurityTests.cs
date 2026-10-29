using System.Text.Json;
using FluentAssertions;
using UCB_Hold.Server.Modules.Usuarios.Application;

namespace UCB_Hold.Tests.Unit.Modules.Usuarios;

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
