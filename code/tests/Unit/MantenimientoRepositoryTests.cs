using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Mantenimiento;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace IMT_Reservas.Tests.Unit.Repositories;

[TestFixture]
public class MantenimientoRepositoryTests
{
    private ApplicationDbContext _dbContext = null!;
    private MantenimientoRepository _repository = null!;
    private MantenimientoMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mapper = new MantenimientoMapper();
        _repository = new MantenimientoRepository(_dbContext, _mapper);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task GetAll_ReturnsJoinedData()
    {
        var empresa = new EmpresaMantenimiento { Id = 1, Nombre = "TechFix", EstadoEliminado = false };
        var mantenimiento = new Mantenimiento { Id = 1, IdEmpresa = 1, Costo = 100, EstadoEliminado = false };
        _dbContext.EmpresasMantenimiento.Add(empresa);
        _dbContext.Mantenimientos.Add(mantenimiento);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetAll();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().NombreEmpresaMantenimiento.Should().Be("TechFix");
    }

    [Test]
    public async Task Get_ExistingId_ReturnsJoinedData()
    {
        var empresa = new EmpresaMantenimiento { Id = 1, Nombre = "TechFix", EstadoEliminado = false };
        var mantenimiento = new Mantenimiento { Id = 1, IdEmpresa = 1, Costo = 100, EstadoEliminado = false };
        _dbContext.EmpresasMantenimiento.Add(empresa);
        _dbContext.Mantenimientos.Add(mantenimiento);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.Get(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.NombreEmpresaMantenimiento.Should().Be("TechFix");
    }

    [Test]
    public async Task Get_NonExistingId_ReturnsNotFound()
    {
        var result = await _repository.Get(99);

        result.IsSuccess.Should().BeFalse();
    }

    [Test]
    public async Task Delete_ValidId_SoftDeletes()
    {
        var mantenimiento = new Mantenimiento { Id = 1, Costo = 100, EstadoEliminado = false };
        _dbContext.Mantenimientos.Add(mantenimiento);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.Delete(1);

        result.IsSuccess.Should().BeTrue();
        var deleted = await _dbContext.Mantenimientos.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == 1);
        deleted!.EstadoEliminado.Should().BeTrue();
    }

    [Test]
    public async Task AddDetalles_AddsRecords()
    {
        var mantenimiento = new Mantenimiento { Id = 1, Costo = 100, EstadoEliminado = false };
        var equipo = new Equipo { Id = 10, CodigoImt = 123, NumeroSerial = "S1", EstadoEliminado = false };
        _dbContext.Mantenimientos.Add(mantenimiento);
        _dbContext.Equipos.Add(equipo);
        await _dbContext.SaveChangesAsync();

        await _repository.AddDetalles(1, [123], ["Preventivo"], ["Limpio"]);

        var detalles = await _dbContext.DetallesMantenimientos.ToListAsync();
        detalles.Should().HaveCount(1);
        detalles.First().IdEquipo.Should().Be(10);
        detalles.First().TipoMantenimiento.Should().Be("Preventivo");
    }

    [Test]
    public async Task HasScheduleConflict_PendingLoanForEquipment_ReturnsTrue()
    {
        var start = DateTime.UtcNow.AddDays(1);
        var end = start.AddHours(2);
        var equipment = new Equipo
        {
            Id = 10,
            CodigoImt = 123,
            EstadoEquipo = EstadoEquipo.Operativo,
            FechaIngresoEquipo = DateOnly.FromDateTime(DateTime.Today),
        };
        var loan = new Prestamo
        {
            EstadoPrestamo = EstadoPrestamo.Pendiente,
            FechaSolicitud = DateTime.UtcNow,
            FechaPrestamoEsperada = start,
            FechaDevolucionEsperada = end,
        };
        _dbContext.AddRange(equipment, loan);
        await _dbContext.SaveChangesAsync();
        _dbContext.DetallesPrestamos.Add(new DetallePrestamo
        {
            IdPrestamo = loan.Id,
            IdEquipo = equipment.Id,
        });
        await _dbContext.SaveChangesAsync();

        var result = await _repository.HasScheduleConflict([123], start, end);

        result.Should().BeTrue();
    }
}
