using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Carrito;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.Extensions.Logging.Abstractions;
using IMT_Reservas.Tests.Helpers;
namespace IMT_Reservas.Tests.Integration;

[TestFixture]
internal class CarritoServiceTests : ServiceTest<CarritoService>
{
    private const int GrupoId = 1;
    private const int EquipoId = 1;
    private const int Total = 2;
    private static readonly DateTime BusinessStart = new(
        2030,
        1,
        7,
        9,
        0,
        0,
        DateTimeKind.Unspecified
    );

    protected override CarritoService CreateService(ApplicationDbContext db)
    {
        var repo = new CarritoRepository(db);
        var configRepo = new ConfiguracionRepository(db, Cache);

        return new CarritoService(repo, NullLogger<CarritoService>.Instance, configRepo);
    }

    [SetUp]
    public async Task SeedBaseData()
    {
        Db.GruposEquipos.Add(new GrupoEquipo
        {
            Id = GrupoId,
            Nombre = "Grupo Test",
            Modelo = "M1",
            Marca = "Marca",
            IdCategoria = 1,
            Cantidad = Total
        });

        Db.Equipos.AddRange(
            new Equipo { Id = EquipoId, IdGrupoEquipo = GrupoId, CodigoImt = 1, EstadoEquipo = EstadoEquipo.Operativo, FechaIngresoEquipo = DateOnly.FromDateTime(DateTime.Today), EstadoEliminado = false },
            new Equipo { Id = EquipoId + 1, IdGrupoEquipo = GrupoId, CodigoImt = 2, EstadoEquipo = EstadoEquipo.Operativo, FechaIngresoEquipo = DateOnly.FromDateTime(DateTime.Today), EstadoEliminado = false }
        );

        await Db.SaveChangesAsync();
    }

    [Test]
    public async Task GetDisponibilidad_NoLoans_ReturnsFullCapacityEveryDay()
    {
        var fechaInicio = BusinessStart;
        var fechaFin = BusinessStart.AddDays(2);
        var request = BuildRequest([GrupoId], fechaInicio, fechaFin);

        var result = await Sut.GetDisponibilidad(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(d => d.CantidadDisponible.Should().Be(Total));
    }

    [Test]
    public async Task GetDisponibilidad_WhenDurationExceedsGroupMaximum_ReturnsError()
    {
        var group = await Db.GruposEquipos.FindAsync(GrupoId);
        group!.TiempoMaximoPrestamoDias = 1;
        await Db.SaveChangesAsync();
        var start = BusinessStart;

        var result = await Sut.GetDisponibilidad(
            BuildRequest([GrupoId], start, start.AddDays(1).AddMinutes(1))
        );

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("hasta 1 día"));
    }

    [Test]
    public async Task GetDisponibilidad_OutsideServiceHours_ReturnsError()
    {
        var beforeOpening = await Sut.GetDisponibilidad(
            new CarritoDto
            {
                ArrayIds = [GrupoId],
                FechaInicio = DateTime.SpecifyKind(
                    DateTime.UtcNow.Date.AddDays(1).AddHours(11).AddMinutes(30),
                    DateTimeKind.Utc
                ),
                FechaFin = DateTime.SpecifyKind(
                    DateTime.UtcNow.Date.AddDays(1).AddHours(12),
                    DateTimeKind.Utc
                ),
            }
        );
        var afterClosing = await Sut.GetDisponibilidad(
            new CarritoDto
            {
                ArrayIds = [GrupoId],
                FechaInicio = DateTime.SpecifyKind(
                    DateTime.UtcNow.Date.AddDays(1).AddHours(21).AddMinutes(30),
                    DateTimeKind.Utc
                ),
                FechaFin = DateTime.SpecifyKind(
                    DateTime.UtcNow.Date.AddDays(1).AddHours(22).AddMinutes(30),
                    DateTimeKind.Utc
                ),
            }
        );

        beforeOpening.IsSuccess.Should().BeFalse();
        afterClosing.IsSuccess.Should().BeFalse();
        beforeOpening.Errors.Should().Contain(error => error.Contains("08:00 a 18:00"));
        afterClosing.Errors.Should().Contain(error => error.Contains("08:00 a 18:00"));
    }

    [Test]
    public async Task GetDisponibilidad_OnSunday_ReturnsError()
    {
        var sunday = new DateTime(2030, 1, 6, 9, 0, 0, DateTimeKind.Unspecified);

        var result = await Sut.GetDisponibilidad(
            BuildRequest([GrupoId], sunday, sunday.AddMinutes(30))
        );

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Contains("lunes a sábado"));
    }

    [Test]
    public async Task GetDisponibilidad_OneActiveLoan_ReducesCapacity()
    {
        var fechaInicio = BusinessStart;
        var fechaFin = BusinessStart.AddDays(2);
        await SeedLoan(EstadoPrestamo.Activo, EquipoId, fechaInicio, fechaFin);
        var request = BuildRequest([GrupoId], fechaInicio, fechaFin);

        var result = await Sut.GetDisponibilidad(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(d => d.CantidadDisponible.Should().Be(Total - 1));
    }

    [Test]
    public async Task GetDisponibilidad_AprobadoLoan_ReducesCapacity()
    {
        var fechaInicio = BusinessStart;
        var fechaFin = BusinessStart.AddDays(2);
        await SeedLoan(EstadoPrestamo.Aprobado, EquipoId, fechaInicio, fechaFin);
        var request = BuildRequest([GrupoId], fechaInicio, fechaFin);

        var result = await Sut.GetDisponibilidad(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(d => d.CantidadDisponible.Should().Be(Total - 1));
    }

    [Test]
    public async Task GetDisponibilidad_PendienteLoan_ReducesCapacity()
    {
        var fechaInicio = BusinessStart;
        var fechaFin = BusinessStart.AddDays(2);
        await SeedLoan(EstadoPrestamo.Pendiente, EquipoId, fechaInicio, fechaFin);
        var request = BuildRequest([GrupoId], fechaInicio, fechaFin);

        var result = await Sut.GetDisponibilidad(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(d => d.CantidadDisponible.Should().Be(Total - 1));
    }

    [Test]
    public async Task GetDisponibilidad_LoanOutsideDateRange_DoesNotReduceCapacity()
    {
        await SeedLoan(
            EstadoPrestamo.Aprobado,
            EquipoId,
            BusinessStart.AddDays(10),
            BusinessStart.AddDays(15)
        );
        var request = BuildRequest([GrupoId], BusinessStart, BusinessStart.AddDays(3));

        var result = await Sut.GetDisponibilidad(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().AllSatisfy(d => d.CantidadDisponible.Should().Be(Total));
    }

    [Test]
    public async Task GetDisponibilidad_EmptyIds_ReturnsEmptyList()
    {
        var request = BuildRequest([], BusinessStart, BusinessStart.AddDays(2));

        var result = await Sut.GetDisponibilidad(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task GetDisponibilidad_NullDates_ReturnsEmptyList()
    {
        var request = new CarritoDto { ArrayIds = [GrupoId], FechaInicio = null, FechaFin = null };

        var result = await Sut.GetDisponibilidad(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Test]
    public async Task GetDisponibilidad_TwoGroups_CalculatesIndependently()
    {
        const int GrupoId2 = 2;
        const int EquipoId3 = 3;
        const int Total2 = 1;

        Db.GruposEquipos.Add(new GrupoEquipo { Id = GrupoId2, Nombre = "Grupo B", Modelo = "M2", Marca = "Marca", IdCategoria = 1, Cantidad = Total2 });
        Db.Equipos.Add(new Equipo { Id = EquipoId3, IdGrupoEquipo = GrupoId2, CodigoImt = 3, EstadoEquipo = EstadoEquipo.Operativo, FechaIngresoEquipo = DateOnly.FromDateTime(DateTime.Today), EstadoEliminado = false });
        await Db.SaveChangesAsync();

        var fechaInicio = BusinessStart;
        var fechaFin = fechaInicio.AddMinutes(30);
        await SeedLoan(EstadoPrestamo.Activo, EquipoId, fechaInicio, fechaFin);
        var request = BuildRequest([GrupoId, GrupoId2], fechaInicio, fechaFin);

        var result = await Sut.GetDisponibilidad(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Single(d => d.IdGrupoEquipo == GrupoId).CantidadDisponible.Should().Be(Total - 1);
        result.Value.Single(d => d.IdGrupoEquipo == GrupoId2).CantidadDisponible.Should().Be(Total2);
    }

    private async Task SeedLoan(EstadoPrestamo estado, int equipoId, DateTime inicio, DateTime fin)
    {
        var prestamo = new Prestamo
        {
            EstadoPrestamo = estado,
            FechaSolicitud = DateTime.UtcNow,
            FechaPrestamoEsperada = inicio,
            FechaDevolucionEsperada = fin,
            EstadoEliminado = false
        };
        Db.Prestamos.Add(prestamo);
        await Db.SaveChangesAsync();

        Db.DetallesPrestamos.Add(new DetallePrestamo { IdPrestamo = prestamo.Id, IdEquipo = equipoId, EstadoEliminado = false });
        await Db.SaveChangesAsync();
    }

    private static CarritoDto BuildRequest(List<int> grupoIds, DateTime inicio, DateTime fin)
    {
        var duration = fin - inicio;
        var startMinutes = inicio.Hour * 60 + inicio.Minute;
        var endMinutes = fin.Hour * 60 + fin.Minute;
        var isWithinServiceHours =
            startMinutes >= 8 * 60
            && startMinutes <= 17 * 60 + 30
            && endMinutes >= 8 * 60
            && endMinutes <= 18 * 60;
        var normalizedStart = DateTime.SpecifyKind(
            isWithinServiceHours ? inicio : inicio.Date.AddHours(9),
            DateTimeKind.Unspecified
        );

        return new CarritoDto
        {
            ArrayIds = grupoIds,
            FechaInicio = normalizedStart,
            FechaFin = normalizedStart + duration,
        };
    }
}
