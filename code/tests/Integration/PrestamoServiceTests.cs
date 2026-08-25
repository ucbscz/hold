using FluentAssertions;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Application.Features.Notificacion;
using IMT_Reservas.Server.Application.Features.Usuario;
using Microsoft.AspNetCore.Http;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Application.Features.Contrato;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using IMT_Reservas.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
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
        var configRepo = new ConfiguracionRepository(db, Cache);
        var mapper = new PrestamoMapper();
        var repo = new PrestamoRepository(db, mapper, new ContractHtmlProcessor());
        var validator = new PrestamoValidator(db, configRepo);

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
            availabilityRepository,
            configRepo
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
            CodigoUcb = "UCB-001",
            NumeroSerial = "SER-001",
            EstadoEquipo = EstadoEquipo.Operativo,
            FechaIngresoEquipo = DateOnly.FromDateTime(DateTime.Today),
            EstadoEliminado = false
        });

        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task Create_WithAvailableEquipo_ReturnsSuccess()
    {
        var start = DateTime.UtcNow.AddDays(1);
        var dto = BuildValidPrestamo(Carnet, GrupoId, start, start.AddDays(3));

        var result = await Sut.Create(dto);

        result.IsSuccess.Should().BeTrue();
        Db.DetallesPrestamos.Should().HaveCount(1);
        Db.DetallesPrestamos.Single().IdEquipo.Should().Be(EquipoId);
    }

    [Test]
    public async Task Create_OutsideServiceHours_ReturnsError()
    {
        var dto = BuildValidPrestamo(
            Carnet,
            GrupoId,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddMinutes(30)
        );
        dto.FechaPrestamoEsperada = DateTime.SpecifyKind(
            DateTime.UtcNow.Date.AddDays(1).AddHours(11).AddMinutes(30),
            DateTimeKind.Utc
        );
        dto.FechaDevolucionEsperada = DateTime.SpecifyKind(
            DateTime.UtcNow.Date.AddDays(1).AddHours(12),
            DateTimeKind.Utc
        );

        var result = await Sut.Create(dto);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors
            .Select(error => error.ErrorMessage)
            .Should()
            .Contain(error => error.Contains("08:00 a 18:00"));
        Db.Prestamos.Should().BeEmpty();
    }

    [Test]
    public async Task Create_OnSunday_ReturnsError()
    {
        var sunday = DateTime.UtcNow.Date.AddDays(1);
        while (sunday.DayOfWeek != DayOfWeek.Sunday)
            sunday = sunday.AddDays(1);

        var start = DateTime.SpecifyKind(sunday.AddHours(13), DateTimeKind.Utc);
        var dto = BuildValidPrestamo(Carnet, GrupoId, start, start.AddMinutes(30));
        dto.FechaPrestamoEsperada = start;
        dto.FechaDevolucionEsperada = start.AddMinutes(30);

        var result = await Sut.Create(dto);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors
            .Select(error => error.ErrorMessage)
            .Should()
            .Contain(error => error.Contains("lunes a sábado"));
        Db.Prestamos.Should().BeEmpty();
    }

    [Test]
    public async Task Create_WhenDurationExceedsGroupMaximum_ReturnsError()
    {
        var group = await Db.GruposEquipos.FindAsync(GrupoId);
        group!.TiempoMaximoPrestamoDias = 2;
        await Db.SaveChangesAsync();
        var start = DateTime.UtcNow.AddDays(1);

        var result = await Sut.Create(
            BuildValidPrestamo(Carnet, GrupoId, start, start.AddDays(2).AddMinutes(1))
        );

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("hasta 2 día"));
        Db.Prestamos.Should().BeEmpty();
    }

    [Test]
    public async Task Create_WhenDurationMatchesGroupMaximum_ReturnsSuccess()
    {
        var group = await Db.GruposEquipos.FindAsync(GrupoId);
        group!.TiempoMaximoPrestamoDias = 2;
        await Db.SaveChangesAsync();
        var start = DateTime.UtcNow.AddDays(1);

        var result = await Sut.Create(
            BuildValidPrestamo(Carnet, GrupoId, start, start.AddDays(2))
        );

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Create_WhenGroupDoesNotExist_ReturnsErrorWithoutPersistingLoan()
    {
        var start = DateTime.UtcNow.AddDays(1);

        var result = await Sut.Create(
            BuildValidPrestamo(Carnet, 999_999, start, start.AddHours(1))
        );

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("no existen"));
        Db.Prestamos.Should().BeEmpty();
    }

    [Test]
    public async Task Create_EquipoAprobadoInSameDates_ReturnsError()
    {
        var fechaInicio = DateTime.UtcNow.AddDays(1);
        var fechaFin = fechaInicio.AddDays(3);
        await SeedActiveLoan(EstadoPrestamo.Aprobado, fechaInicio, fechaFin);

        var result = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, fechaInicio, fechaFin));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("disponible"));
    }

    [Test]
    public async Task Create_EquipoActivoInSameDates_ReturnsError()
    {
        var fechaInicio = DateTime.UtcNow.AddDays(1);
        var fechaFin = fechaInicio.AddDays(3);
        await SeedActiveLoan(EstadoPrestamo.Activo, fechaInicio, fechaFin);

        var result = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, fechaInicio, fechaFin));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("disponible"));
    }

    [Test]
    public async Task Create_EquipoPendienteInSameDates_ReturnsError()
    {
        var fechaInicio = DateTime.UtcNow.AddDays(1);
        var fechaFin = fechaInicio.AddDays(3);
        await SeedActiveLoan(EstadoPrestamo.Pendiente, fechaInicio, fechaFin);

        var result = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, fechaInicio, fechaFin));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("disponible"));
    }

    [Test]
    public async Task Create_EquipoLoanedOnDifferentDates_DoesNotBlock()
    {
        await SeedActiveLoan(EstadoPrestamo.Aprobado, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(15));

        var start = DateTime.UtcNow.AddDays(1);
        var result = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, start, start.AddDays(3)));

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task UpdateStatus_ValidTransition_Succeeds()
    {
        var start = DateTime.UtcNow.AddDays(1);
        var createResult = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, start, start.AddDays(3)));
        var prestamoId = createResult.Value.Id!.Value;

        var result = await Sut.UpdateStatus(prestamoId, "rechazado");

        result.IsSuccess.Should().BeTrue();
        result.Value.EstadoPrestamo.Should().Be("rechazado");
    }

    [Test]
    public async Task UpdateStatus_InvalidTransition_ReturnsError()
    {
        var start = DateTime.UtcNow.AddDays(1);
        var createResult = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, start, start.AddDays(3)));
        var prestamoId = createResult.Value.Id!.Value;

        var result = await Sut.UpdateStatus(prestamoId, "activo");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("no permitida"));
    }

    [Test]
    public async Task UpdateStatus_UnknownStatus_ReturnsError()
    {
        var start = DateTime.UtcNow.AddDays(1);
        var createResult = await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, start, start.AddDays(3)));
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
        var fechaInicio = DateTime.UtcNow.AddDays(1);
        var fechaFin = DateTime.UtcNow.AddDays(3);

        var loan = new Prestamo
        {
            Carnet = Carnet,
            EstadoPrestamo = EstadoPrestamo.Pendiente,
            FechaSolicitud = DateTime.UtcNow,
            FechaPrestamoEsperada = fechaInicio,
            FechaDevolucionEsperada = fechaFin,
        };
        Db.Prestamos.Add(loan);
        await Db.SaveChangesAsync();
        Db.DetallesPrestamos.Add(new DetallePrestamo
        {
            IdPrestamo = loan.Id,
            IdGrupoEquipo = GrupoId,
        });
        await Db.SaveChangesAsync();

        await SeedActiveLoanForEquipo(EquipoId, EstadoPrestamo.Aprobado, fechaInicio, fechaFin);

        var result = await Sut.UpdateStatus(loan.Id, "aprobado");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("no hay equipos disponibles"));
    }

    [Test]
    public async Task UpdateStatus_ApproveWithoutConflict_Succeeds()
    {
        var createResult = await Sut.Create(BuildValidPrestamo(
            Carnet,
            GrupoId,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(3)
        ));
        var prestamoId = createResult.Value.Id!.Value;

        var result = await Sut.UpdateStatus(prestamoId, "aprobado");

        result.IsSuccess.Should().BeTrue();
        result.Value.EstadoPrestamo.Should().Be("aprobado");
    }

    [Test]
    public async Task UpdateStatus_ManualApproval_UsesActorNameAsNotificationEmitter()
    {
        var createResult = await Sut.Create(
            BuildValidPrestamo(
                Carnet,
                GrupoId,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(3)
            )
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
    public async Task UpdateStatus_ApproveAfterExpectedStart_ReturnsError()
    {
        var loan = new Prestamo
        {
            Carnet = Carnet,
            EstadoPrestamo = EstadoPrestamo.Pendiente,
            FechaSolicitud = DateTime.UtcNow.AddHours(-3),
            FechaPrestamoEsperada = DateTime.UtcNow.AddHours(-2),
            FechaDevolucionEsperada = DateTime.UtcNow.AddHours(1),
        };
        Db.Prestamos.Add(loan);
        await Db.SaveChangesAsync();

        var result = await Sut.UpdateStatus(loan.Id, "aprobado");

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("ya venció"));
    }

    [Test]
    public async Task UpdateStatus_Finalized_PreservesManualUserBlock()
    {
        const string manualReason = "Bloqueo administrativo vigente";
        var loanId = await SeedOverdueLoan(manualReason);

        var result = await Sut.UpdateStatus(loanId, "finalizado");

        result.IsSuccess.Should().BeTrue();
        var user = await Db.Usuarios.AsNoTracking().SingleAsync(u => u.Carnet == Carnet);
        user.Bloqueado.Should().BeTrue();
        user.MotivoBloqueo.Should().Be(manualReason);
        Db.AuditLogs.Should().NotContain(log =>
            log.Entidad == "Usuario" && log.Accion == AuditAccion.Desbloquear.ToString()
        );
    }

    [Test]
    public async Task UpdateStatus_Finalized_RemovesAndAuditsAutomaticUserBlock()
    {
        var loanId = await SeedOverdueLoan(AutomaticBlockReasons.OverdueLoan);

        var result = await Sut.UpdateStatus(loanId, "finalizado");

        result.IsSuccess.Should().BeTrue();
        var user = await Db.Usuarios.AsNoTracking().SingleAsync(u => u.Carnet == Carnet);
        user.Bloqueado.Should().BeFalse();
        user.MotivoBloqueo.Should().BeNull();
        Db.AuditLogs.Should().ContainSingle(log =>
            log.Entidad == "Usuario"
            && log.EntidadId == Carnet
            && log.Accion == AuditAccion.Desbloquear.ToString()
            && log.AdminNombre == "Sistema"
        );
    }

    [Test]
    public async Task Create_WritesAssignedEquipmentIntoContractImmediately()
    {
        const string contract = """
            <table><tr>
              <td class="imt-code" data-grupo-id="1">Pendiente de asignación</td>
              <td class="ucb-code" data-grupo-id="1">Pendiente de asignación</td>
              <td class="serial-code" data-grupo-id="1">Pendiente de asignación</td>
            </tr></table>
            """;
        var dto = BuildValidPrestamo(
            Carnet,
            GrupoId,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(3)
        );
        dto.Contrato = contract;
        var createResult = await Sut.Create(dto);

        createResult.IsSuccess.Should().BeTrue();
        var savedContract = Db.Contratos.Single().ContratoHtml;
        savedContract.Should().Contain(">1</td>");
        savedContract.Should().Contain(">UCB-001</td>");
        savedContract.Should().Contain(">SER-001</td>");
        savedContract.Should().NotContain("Pendiente de asignación");
    }

    [Test]
    public async Task Create_WritesEveryPhysicalCodeForMultipleUnits()
    {
        Db.Equipos.Add(new Equipo
        {
            Id = 2,
            IdGrupoEquipo = GrupoId,
            CodigoImt = 2,
            CodigoUcb = null,
            NumeroSerial = "SER-002",
            EstadoEquipo = EstadoEquipo.Operativo,
            FechaIngresoEquipo = DateOnly.FromDateTime(DateTime.Today),
        });
        await Db.SaveChangesAsync();
        const string contract = """
            <table><tbody><tr>
              <td class="imt-code" data-grupo-id="1">Por definirse</td>
              <td class="ucb-code" data-grupo-id="1">Por definirse</td>
              <td class="serial-code" data-grupo-id="1">Por definirse</td>
            </tr></tbody></table>
            """;
        var dto = BuildValidPrestamo(
            Carnet,
            GrupoId,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(3)
        );
        dto.GrupoEquipoId = [GrupoId, GrupoId];
        dto.Contrato = contract;

        var result = await Sut.Create(dto);

        result.IsSuccess.Should().BeTrue();
        var savedContract = Db.Contratos.Single().ContratoHtml;
        savedContract.Should().Contain(">1, 2</td>");
        savedContract.Should().Contain(">UCB-001, No registrado</td>");
        savedContract.Should().Contain(">SER-001, SER-002</td>");
    }

    [Test]
    public async Task CreateForUser_OverridesSpoofedCarnet()
    {
        Db.Usuarios.Add(new Usuario
        {
            Carnet = "U002",
            Nombre = "Other",
            ApellidoPaterno = "User",
            Email = "u002@ucb.edu.bo",
            Contrasena = "hashed",
        });
        await Db.SaveChangesAsync();
        var dto = BuildValidPrestamo(
            "U002",
            GrupoId,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(3)
        );

        var result = await Sut.CreateForUser(dto, Carnet);

        result.IsSuccess.Should().BeTrue();
        Db.Prestamos.Single().Carnet.Should().Be(Carnet);
    }

    [Test]
    public async Task GetAuthorized_HidesAnotherUsersLoan()
    {
        var createResult = await Sut.Create(
            BuildValidPrestamo(
                Carnet,
                GrupoId,
                DateTime.UtcNow.AddDays(1),
                DateTime.UtcNow.AddDays(3)
            )
        );
        var id = createResult.Value.Id!.Value;

        var unauthorized = await Sut.GetAuthorized(id, "U002", false);
        var admin = await Sut.GetAuthorized(id, "U002", true);

        unauthorized.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
        admin.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Create_SanitizesUnsafeContractMarkup()
    {
        var dto = BuildValidPrestamo(
            Carnet,
            GrupoId,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(3)
        );
        dto.Contrato = """
            <div onclick="alert(1)"><script>alert(1)</script>
              <img src="javascript:alert(1)" onerror="alert(2)">
              <table><tbody><tr>
                <td class="imt-code" data-grupo-id="1">Por definirse</td>
              </tr></tbody></table>
            </div>
            """;

        var result = await Sut.Create(dto);

        result.IsSuccess.Should().BeTrue();
        var savedContract = Db.Contratos.Single().ContratoHtml;
        savedContract.Should().NotContain("script");
        savedContract.Should().NotContain("onclick");
        savedContract.Should().NotContain("onerror");
        savedContract.Should().NotContain("javascript:");
        savedContract.Should().Contain(">1</td>");
    }

    [Test]
    public async Task GetHistory_ByCarnet_ReturnsOnlyThatUsersPrestamos()
    {
        var start = DateTime.UtcNow.AddDays(1);
        await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, start, start.AddDays(3)));

        var result = await Sut.GetHistory(Carnet, "todos");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(p => p.CarnetUsuario.Should().Be(Carnet));
    }

    [Test]
    public async Task GetHistory_FilterByEstado_ReturnsFiltered()
    {
        var start = DateTime.UtcNow.AddDays(1);
        await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, start, start.AddDays(3)));

        var result = await Sut.GetHistory(Carnet, "pendiente");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(p => p.EstadoPrestamo.Should().Be("pendiente"));
    }

    [Test]
    public async Task GetFiltered_WithoutCarnet_FiltersAllLoansByStatus()
    {
        var start = DateTime.UtcNow.AddDays(1);
        await Sut.Create(BuildValidPrestamo(Carnet, GrupoId, start, start.AddDays(3)));

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

    private async Task<int> SeedOverdueLoan(string blockReason)
    {
        var user = await Db.Usuarios.SingleAsync(u => u.Carnet == Carnet);
        user.Bloqueado = true;
        user.MotivoBloqueo = blockReason;

        var loan = new Prestamo
        {
            Carnet = Carnet,
            EstadoPrestamo = EstadoPrestamo.Atrasado,
            FechaSolicitud = DateTime.UtcNow.AddHours(-3),
            FechaPrestamoEsperada = DateTime.UtcNow.AddHours(-2),
            FechaDevolucionEsperada = DateTime.UtcNow.AddHours(-1),
            EstadoEliminado = false
        };
        Db.Prestamos.Add(loan);
        await Db.SaveChangesAsync();

        return loan.Id;
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

    private static PrestamoDto BuildValidPrestamo(
        string carnet,
        int grupoId,
        DateTime inicio,
        DateTime fin
    )
    {
        var duration = fin - inicio;
        var localDate = inicio.Date;
        while (
            localDate.DayOfWeek == DayOfWeek.Sunday
            || localDate.Add(duration).DayOfWeek == DayOfWeek.Sunday
        )
            localDate = localDate.AddDays(1);

        var normalizedStart = DateTime.SpecifyKind(
            localDate.AddHours(13),
            DateTimeKind.Utc
        );

        return new PrestamoDto
        {
            CarnetUsuario = carnet,
            GrupoEquipoId = [grupoId],
            FechaPrestamoEsperada = normalizedStart,
            FechaDevolucionEsperada = normalizedStart + duration,
        };
    }
}
