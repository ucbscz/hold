using System.Text.Json;
using FluentAssertions;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Application.Features.Usuario;
using IMT_Reservas.Server.Application.Features.Equipo;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
internal class AuditChangeDetailTests
{
    [Test]
    public void Build_RecordsTraceableValuesAndProtectsStoredDocuments()
    {
        var previous = new UsuarioDto
        {
            Nombre = "Ana",
            Email = "anterior@ucb.edu.bo",
            NombreReferencia = "María Pérez",
            ImagenFirma = [1, 2],
            Rol = "estudiante",
        };
        var current = new UsuarioDto
        {
            Nombre = "Andrea",
            Email = "nuevo@ucb.edu.bo",
            NombreReferencia = "Carlos Rojas",
            ImagenFirma = [3, 4],
            Rol = "docente",
        };

        var detail = AuditChangeDetail.Build(previous, current);
        using var json = JsonDocument.Parse(detail!);
        var changes = json.RootElement.GetProperty("cambios").EnumerateArray().ToList();

        changes.Should().Contain(change =>
            change.GetProperty("campo").GetString() == "Nombre"
            && change.GetProperty("anterior").GetString() == "Ana"
            && change.GetProperty("nuevo").GetString() == "Andrea"
            && !change.GetProperty("protegido").GetBoolean()
        );
        changes.Should().Contain(change =>
            change.GetProperty("campo").GetString() == "Email"
            && !change.GetProperty("protegido").GetBoolean()
            && change.GetProperty("anterior").GetString() == "anterior@ucb.edu.bo"
            && change.GetProperty("nuevo").GetString() == "nuevo@ucb.edu.bo"
        );
        changes.Should().Contain(change =>
            change.GetProperty("campo").GetString() == "Nombre referencia"
            && !change.GetProperty("protegido").GetBoolean()
            && change.GetProperty("anterior").GetString() == "María Pérez"
            && change.GetProperty("nuevo").GetString() == "Carlos Rojas"
        );
        changes.Should().Contain(change =>
            change.GetProperty("campo").GetString() == "Firma"
            && change.GetProperty("protegido").GetBoolean()
        );
    }

    [Test]
    public void BuildCreated_RecordsTraceableSnapshotWithoutStoredDocuments()
    {
        var detail = AuditChangeDetail.BuildCreated(new UsuarioDto
        {
            Nombre = "Ana",
            Email = "ana@ucb.edu.bo",
            ImagenFirma = [1, 2],
            Rol = "docente",
        });

        using var json = JsonDocument.Parse(detail!);
        var changes = json.RootElement.GetProperty("cambios").EnumerateArray().ToList();

        changes.Should().Contain(change =>
            change.GetProperty("campo").GetString() == "Nombre"
            && change.GetProperty("nuevo").GetString() == "Ana"
        );
        changes.Should().Contain(change =>
            change.GetProperty("campo").GetString() == "Email"
            && !change.GetProperty("protegido").GetBoolean()
            && change.GetProperty("nuevo").GetString() == "ana@ucb.edu.bo"
        );
    }

    [Test]
    public void BuildDeleted_RecordsOnlyPreviousVisibleValues()
    {
        var detail = AuditChangeDetail.BuildDeleted(new UsuarioDto
        {
            Nombre = "Ana",
            Rol = "docente",
        });

        using var json = JsonDocument.Parse(detail!);
        var name = json.RootElement.GetProperty("cambios").EnumerateArray()
            .Single(change => change.GetProperty("campo").GetString() == "Nombre");

        name.GetProperty("anterior").GetString().Should().Be("Ana");
        name.GetProperty("nuevo").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Test]
    public void Build_UsesRelatedNamesInsteadOfInternalIdentifiers()
    {
        var detail = AuditChangeDetail.Build(
            new EquipoDto { IdAmbiente = 1, Ubicacion = "Laboratorio A" },
            new EquipoDto { IdAmbiente = 2, Ubicacion = "Laboratorio B" }
        );

        using var json = JsonDocument.Parse(detail!);
        var changes = json.RootElement.GetProperty("cambios").EnumerateArray().ToList();

        changes.Should().ContainSingle(change =>
            change.GetProperty("campo").GetString() == "Ubicacion"
            && change.GetProperty("anterior").GetString() == "Laboratorio A"
            && change.GetProperty("nuevo").GetString() == "Laboratorio B"
        );
        changes.Should().NotContain(change => change.GetProperty("campo").GetString() == "Ambiente");
    }

    [Test]
    public void Build_NeverExposesAuthenticationSecrets()
    {
        var detail = AuditChangeDetail.Build(
            new CredentialAudit("1234567", "hash-anterior", "refresh-anterior", "token-anterior"),
            new CredentialAudit("7654321", "hash-nuevo", "refresh-nuevo", "token-nuevo")
        );

        using var json = JsonDocument.Parse(detail!);
        var changes = json.RootElement.GetProperty("cambios").EnumerateArray().ToList();

        changes.Should().HaveCount(4);
        changes.Should().OnlyContain(change => change.GetProperty("protegido").GetBoolean());
        detail.Should().NotContain("hash-anterior");
        detail.Should().NotContain("hash-nuevo");
        detail.Should().NotContain("refresh-anterior");
        detail.Should().NotContain("refresh-nuevo");
        detail.Should().NotContain("token-anterior");
        detail.Should().NotContain("token-nuevo");
    }

    private sealed record CredentialAudit(
        string Carnet,
        string Contrasena,
        string RefreshToken,
        string TokenVerificacionHash
    );
}
