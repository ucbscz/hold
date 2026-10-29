using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using IMT_Reservas.Server.Shared.Persistence.RowLevelSecurity;

namespace IMT_Reservas.Tests.Unit.Shared;

[TestFixture]
public class DatabaseIdentityTests
{
    [Test]
    public void BackgroundWork_RunsAsSystem()
    {
        DatabaseIdentity.From(null).Should().Be(new DatabaseIdentity(string.Empty, "sistema"));
    }

    [Test]
    public void UnauthenticatedRequest_IsAnonymous()
    {
        DatabaseIdentity
            .From(new DefaultHttpContext())
            .Should()
            .Be(new DatabaseIdentity(string.Empty, "anonimo"));
    }

    [Test]
    public void AuthenticatedRequest_UsesCarnetAndRole()
    {
        DatabaseIdentity
            .From(Context("E100", "estudiante"))
            .Should()
            .Be(new DatabaseIdentity("E100", "estudiante"));
    }

    [Test]
    public void AuthenticatedRequestToAnonymousEndpoint_IsAnonymous()
    {
        var context = Context("ADM1", "administrador");
        context.SetEndpoint(
            new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new AllowAnonymousAttribute()), "login")
        );

        DatabaseIdentity.From(context).Should().Be(new DatabaseIdentity("ADM1", "anonimo"));
    }

    [Test]
    public void SystemScope_KeepsCarnetAndElevatesRole()
    {
        DatabaseIdentity
            .From(Context("E100", "estudiante"))
            .AsSystem()
            .Should()
            .Be(new DatabaseIdentity("E100", "sistema"));
    }

    private static DefaultHttpContext Context(string carnet, string role) =>
        new()
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(
                    [new Claim("sub", carnet), new Claim("role", role)],
                    "Bearer",
                    "sub",
                    "role"
                )
            ),
        };
}
