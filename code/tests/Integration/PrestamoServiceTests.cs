using FluentAssertions;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Application.Features.Notificacion;
using IMT_Reservas.Server.Application.Features.Usuario;
using Microsoft.AspNetCore.Http;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using IMT_Reservas.Tests.Helpers;
using System.Text.Json;
namespace IMT_Reservas.Tests.Integration;

[TestFixture]
internal class PrestamoServiceTests : ServiceTest<PrestamoService>
{
    private const string Carnet = "U001";
    private const int GrupoId = 1;
    private const int EquipoId = 1;

    protected override PrestamoService CreateService(ApplicationDbContext db)
    {
        var mapper = new PrestamoMapper();
        var repo = new PrestamoRepository(db, mapper);
        var validator = new PrestamoValidator(db);

        var audit = new AuditLogService(new AuditLogRepository(db), new HttpContextAccessor());
        var notifications = new NotificacionService(new NotificacionRepository(db));
        var userRepository = new UsuarioRepository(db, new UsuarioMapper(), repo);
        var availabilityRepository = new AvisoDisponibilidadRepository(db);

        return new PrestamoService(
            repo,
            mapper,
            validator,
            audit,
            notifications,
            userRepository,
            availabilityRepository
        );
    }

    [SetUp]
    public async Task SeedBaseData()
    {
        Db.Usuarios.Add(new Usuario
        {
            Carnet = Carnet,
            Nombre = "Test",
            ApellidoPaterno = "User",
            Email = "u001@ucb.edu.bo",
            Contrasena = "hashed",
            EstadoEliminado = false
        });

        Db.GruposEquipos.Add(new GrupoEquipo
        {
            Id = GrupoId,
            Nombre = "Grupo Test",
            Modelo = "M1",
            Marca = "Marca",
            IdCategoria = 1,
            Cantidad = 1
        });

        Db.Equipos.Add(new Equipo
        {
            Id = EquipoId,
            IdGrupoEquipo = GrupoId,
            CodigoImt = 1,
            EstadoEquipo = EstadoEquipo.Operativo,
            FechaIngresoEquipo = DateOnly.FromDateTime(DateTime.Today),
            EstadoEliminado = false
        });

        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task Create_WithAvailableEquipo_ReturnsSuccess()
    {
        var dto = BuildValidPrestamo(Carnet, GrupoId, DateTime.Today, DateTime.Today.AddDays(3));

        var result = await Sut.Create(dto);

        result.IsSuccess.Should().BeTrue();
        Db.DetallesPrestamos.Should().HaveCount(1);
    }

    [Test]
    public async Task Create_EquipoAprobadoInSameDates_ReturnsError()
    {
        var fechaInicio = DateTime.Today;
        var fechaFin = DateTime.Today.AddDays(3);
        await SeedActiveLoan(EstadoPrestamo.Aprobado, fechaInicio, fechaFin);

        var result = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, fechaInicio, fechaFin));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("disponible"));
    }

    [Test]
    public async Task Create_EquipoActivoInSameDates_ReturnsError()
    {
        var fechaInicio = DateTime.Today;
        var fechaFin = DateTime.Today.AddDays(3);
        await SeedActiveLoan(EstadoPrestamo.Activo, fechaInicio, fechaFin);

        var result = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, fechaInicio, fechaFin));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("disponible"));
    }

    [Test]
    public async Task Create_EquipoPendienteInSameDates_DoesNotBlock()
    {
        var fechaInicio = DateTime.Today;
        var fechaFin = DateTime.Today.AddDays(3);
        await SeedActiveLoan(EstadoPrestamo.Pendiente, fechaInicio, fechaFin);

        var result = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, fechaInicio, fechaFin));

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Create_EquipoLoanedOnDifferentDates_DoesNotBlock()
    {
        await SeedActiveLoan(EstadoPrestamo.Aprobado, DateTime.Today.AddDays(10), DateTime.Today.AddDays(15));

        var result = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, DateTime.Today, DateTime.Today.AddDays(3)));

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UpdateStatus_ValidTransition_Succeeds()
    {
        var createResult = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, DateTime.Today, DateTime.Today.AddDays(3)));
        var prestamoId = createResult.Value.Id!.Value;

        var result = await Sut.UpdateStatus(prestamoId, "rechazado");

        result.IsSuccess.Should().BeTrue();
        result.Value.EstadoPrestamo.Should().Be("rechazado");
    }

    [Test]
    public async Task UpdateStatus_InvalidTransition_ReturnsError()
    {
        var createResult = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, DateTime.Today, DateTime.Today.AddDays(3)));
        var prestamoId = createResult.Value.Id!.Value;

        var result = await Sut.UpdateStatus(prestamoId, "activo");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("no permitida"));
    }

    [Test]
    public async Task UpdateStatus_UnknownStatus_ReturnsError()
    {
        var createResult = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, DateTime.Today, DateTime.Today.AddDays(3)));
        var prestamoId = createResult.Value.Id!.Value;

        var result = await Sut.UpdateStatus(prestamoId, "estado_inventado");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("no reconocido"));
    }

    [Test]
    public async Task UpdateStatus_NonExistentPrestamo_ReturnsNotFound()
    {
        var result = await Sut.UpdateStatus(99, "aprobado");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Test]
    public async Task UpdateStatus_ApproveWithConflict_ReturnsError()
    {
        var fechaInicio = DateTime.Today;
        var fechaFin = DateTime.Today.AddDays(3);

        var createResult = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, fechaInicio, fechaFin));
        var prestamoId = createResult.Value.Id!.Value;

        await SeedActiveLoanForEquipo(EquipoId, EstadoPrestamo.Aprobado, fechaInicio, fechaFin);

        var result = await Sut.UpdateStatus(prestamoId, "aprobado");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("no hay equipos disponibles"));
    }

    [Test]
    public async Task UpdateStatus_ApproveWithoutConflict_Succeeds()
    {
        var createResult = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, DateTime.Today, DateTime.Today.AddDays(3)));
        var prestamoId = createResult.Value.Id!.Value;

        var result = await Sut.UpdateStatus(prestamoId, "aprobado");

        result.IsSuccess.Should().BeTrue();
        result.Value.EstadoPrestamo.Should().Be("aprobado");
    }

    [Test]
    public async Task UpdateStatus_ManualApproval_UsesActorNameAsNotificationEmitter()
    {
        var createResult = await Sut.Create(
            BuildValidPrestamo(Carnet, GrupoId, DateTime.Today, DateTime.Today.AddDays(3))
        );
        var prestamoId = createResult.Value.Id!.Value;

        var result = await Sut.UpdateStatus(
            prestamoId,
            "aprobado",
            actorCarnet: Carnet
        );

        result.IsSuccess.Should().BeTrue();
        var notification = Db.Notificaciones.Single(notification =>
            notification.CarnetUsuario == Carnet
            && notification.Tipo == TipoNotificacion.PrestamoAprobado.ToString()
        );
        using var detail = JsonDocument.Parse(notification.Detalle!);
        detail.RootElement.GetProperty("emisor").GetString().Should().Be("Test User");
    }

    [Test]
    public async Task GetHistory_ByCarnet_ReturnsOnlyThatUsersPrestamos()
    {
        await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, DateTime.Today, DateTime.Today.AddDays(3)));

        var result = await Sut.GetHistory(Carnet, "todos");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(p => p.CarnetUsuario.Should().Be(Carnet));
    }

    [Test]
    public async Task GetHistory_FilterByEstado_ReturnsFiltered()
    {
        await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, DateTime.Today, DateTime.Today.AddDays(3)));

        var result = await Sut.GetHistory(Carnet, "pendiente");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(p => p.EstadoPrestamo.Should().Be("pendiente"));
    }

    [Test]
    public async Task GetFiltered_WithoutCarnet_FiltersAllLoansByStatus()
    {
        await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, DateTime.Today, DateTime.Today.AddDays(3)));

        var result = await Sut.GetFiltered(null, "pendiente");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle();
        result.Value.Should().AllSatisfy(p => p.EstadoPrestamo.Should().Be("pendiente"));
    }

    [Test]
    public async Task GetHistory_EmptyCarnet_ReturnsError()
    {
        var result = await Sut.GetHistory("", "todos");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain("Carnet requerido");
    }

    private async Task SeedActiveLoan(EstadoPrestamo estado, DateTime inicio, DateTime fin)
    {
        var prestamo = new Prestamo
        {
            Carnet = Carnet,
            EstadoPrestamo = estado,
            FechaSolicitud = DateTime.UtcNow,
            FechaPrestamoEsperada = inicio,
            FechaDevolucionEsperada = fin,
            EstadoEliminado = false
        };

        Db.Prestamos.Add(prestamo);
        await Db.SaveChangesAsync();

        Db.DetallesPrestamos.Add(new DetallePrestamo
        {
            IdPrestamo = prestamo.Id,
            IdEquipo = EquipoId,
            EstadoEliminado = false
        });

        await Db.SaveChangesAsync();
    }

    private async Task SeedActiveLoanForEquipo(int equipoId, EstadoPrestamo estado, DateTime inicio, DateTime fin)
    {
        var prestamo = new Prestamo
        {
            Carnet = Carnet,
            EstadoPrestamo = estado,
            FechaSolicitud = DateTime.UtcNow,
            FechaPrestamoEsperada = inicio,
            FechaDevolucionEsperada = fin,
            EstadoEliminado = false
        };
        Db.Prestamos.Add(prestamo);
        await Db.SaveChangesAsync();

        Db.DetallesPrestamos.Add(new DetallePrestamo
        {
            IdPrestamo = prestamo.Id,
            IdEquipo = equipoId,
            EstadoEliminado = false
        });

        await Db.SaveChangesAsync();
    }

    private static PrestamoDto BuildValidPrestamo(string carnet, int grupoId, DateTime inicio, DateTime fin) => new()
    {
        CarnetUsuario = carnet,
        GrupoEquipoId = [grupoId],
        FechaPrestamoEsperada = inicio,
        FechaDevolucionEsperada = fin
    };
}
