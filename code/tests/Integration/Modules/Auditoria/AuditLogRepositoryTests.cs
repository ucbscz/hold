using FluentAssertions;
using IMT_Reservas.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using IMT_Reservas.Server.Modules.Auditoria.Application;
using IMT_Reservas.Server.Modules.Auditoria.Domain;
using IMT_Reservas.Server.Modules.Auditoria.Infrastructure;
using IMT_Reservas.Server.Modules.Inventario.Domain;
using IMT_Reservas.Server.Modules.Usuarios.Domain;
using IMT_Reservas.Server.Modules.Usuarios.Infrastructure;
using IMT_Reservas.Server.Shared.Persistence;

namespace IMT_Reservas.Tests.Integration.Modules.Auditoria;

[TestFixture]
internal class AuditLogRepositoryTests : ServiceTest<AuditLogRepository>
{
    protected override AuditLogRepository CreateService(ApplicationDbContext db) =>
        new(db, TestBranding.Labels);

    [Test]
    public async Task GetFiltered_FiltersByActorActionAndDateBeforeLimitingResults()
    {
        var now = DateTime.UtcNow;
        Db.AuditLogs.AddRange(
            BuildLog("Rechazar", "Fernando Terrazas", "12890061", now),
            BuildLog("Aprobar", "Fernando Terrazas", "12890061", now),
            BuildLog("Rechazar", "Otra Persona", "5555555", now)
        );
        await Db.SaveChangesAsync();

        var result = await Sut.GetFiltered(
            "Prestamo",
            "fernando",
            "Rechazar",
            now.AddMinutes(-1),
            now.AddMinutes(1)
        );

        result.Should().ContainSingle();
        result.Single().AdminCarnet.Should().Be("12890061");
        result.Single().Accion.Should().Be("Rechazar");
    }

    [Test]
    public async Task AuditService_StoresTheCurrentActorsFullName()
    {
        Db.Usuarios.Add(new Usuario
        {
            Carnet = "12890061",
            Nombre = "Fernando",
            ApellidoPaterno = "Terrazas",
            ApellidoMaterno = "Llanos",
        });
        await Db.SaveChangesAsync();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim("sub", "12890061")], "test")
            ),
        };
        var service = new AuditLogService(
            Sut,
            new HttpContextAccessor { HttpContext = context },
            new UsuarioReadRepository(Db)
        );

        await service.Log(AuditAccion.Editar, "Equipo", "15");

        Db.AuditLogs.Single().AdminNombre.Should().Be("Fernando Terrazas Llanos");
    }

    [Test]
    public async Task GetFiltered_ResolvesEquipmentToOperationalCodeAndName()
    {
        Db.GruposEquipos.Add(new GrupoEquipo
        {
            Id = 4,
            Nombre = "Osciloscopio",
            Modelo = "M1",
            Marca = "Marca",
        });
        Db.Equipos.Add(new Equipo
        {
            Id = 15,
            IdGrupoEquipo = 4,
            CodigoImt = 240000015,
        });
        Db.AuditLogs.Add(new AuditLog
        {
            Accion = "Editar",
            Entidad = "Equipo",
            EntidadId = "15",
            AdminNombre = "Administrador",
            AdminCarnet = "100",
        });
        await Db.SaveChangesAsync();

        var result = await Sut.GetFiltered("Equipo", null, null, null, null);

        result.Should().ContainSingle();
        result.Single().EntidadNombre.Should().Be($"{TestBranding.InternalCodeLabel} 240000015 · Osciloscopio");
    }

    [Test]
    public async Task GetFiltered_ResolvesUserWithoutExposingCarnetAsRecordName()
    {
        Db.Usuarios.Add(new Usuario
        {
            Carnet = "12890061",
            Nombre = "Fernando",
            ApellidoPaterno = "Terrazas",
            ApellidoMaterno = "Llanos",
        });
        Db.AuditLogs.Add(new AuditLog
        {
            Accion = "Editar",
            Entidad = "Usuario",
            EntidadId = "12890061",
            AdminNombre = "Administrador",
            AdminCarnet = "100",
        });
        await Db.SaveChangesAsync();

        var result = await Sut.GetFiltered("Usuario", null, null, null, null);

        result.Single().EntidadNombre.Should().Be("Fernando Terrazas Llanos");
    }

    [Test]
    public async Task GetFiltered_ResolvesNamesFromSoftDeletedRecords()
    {
        Db.Categorias.Add(new Categoria
        {
            Id = 7,
            Nombre = "Instrumentación",
            EstadoEliminado = true,
        });
        Db.AuditLogs.Add(new AuditLog
        {
            Accion = "Eliminar",
            Entidad = "Categoria",
            EntidadId = "7",
            AdminNombre = "Administrador",
            AdminCarnet = "100",
        });
        await Db.SaveChangesAsync();

        var result = await Sut.GetFiltered("Categoria", null, null, null, null);

        result.Single().EntidadNombre.Should().Be("Instrumentación");
    }

    private static AuditLog BuildLog(
        string action,
        string actorName,
        string actorCarnet,
        DateTime timestamp
    ) =>
        new()
        {
            Accion = action,
            Entidad = "Prestamo",
            EntidadId = "1",
            AdminNombre = actorName,
            AdminCarnet = actorCarnet,
            Timestamp = timestamp,
            EstadoEliminado = false,
        };
}
