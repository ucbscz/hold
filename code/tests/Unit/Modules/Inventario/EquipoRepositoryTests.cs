using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Equipo;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace IMT_Reservas.Tests.Unit.Repositories;

[TestFixture]
public class EquipoRepositoryTests
{
    private ApplicationDbContext _dbContext = null!;
    private EquipoRepository _repository = null!;
    private EquipoMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mapper = new EquipoMapper();
        _repository = new EquipoRepository(_dbContext, _mapper);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task GetMaxCodigoImt_ReturnsMax()
    {
        _dbContext.Equipos.Add(new Equipo { CodigoImt = 10, NumeroSerial = "E1" });
        _dbContext.Equipos.Add(new Equipo { CodigoImt = 20, NumeroSerial = "E2" });
        await _dbContext.SaveChangesAsync();

        var max = await _repository.GetMaxCodigoImt();
        max.Should().Be(20);
    }

    [Test]
    public async Task GetMaxCodigoImt_Empty_ReturnsZero()
    {
        var max = await _repository.GetMaxCodigoImt();
        max.Should().Be(0);
    }

    [Test]
    public async Task ExistsByCodigoImt_Exists_ReturnsTrue()
    {
        _dbContext.Equipos.Add(new Equipo { CodigoImt = 10, NumeroSerial = "E1", EstadoEliminado = false });
        await _dbContext.SaveChangesAsync();

        var exists = await _repository.ExistsByCodigoImt(10);
        exists.Should().BeTrue();
    }

    [Test]
    public async Task ExistsByCodigoImt_Deleted_ReturnsFalse()
    {
        _dbContext.Equipos.Add(new Equipo { CodigoImt = 10, NumeroSerial = "E1", EstadoEliminado = true });
        await _dbContext.SaveChangesAsync();

        var exists = await _repository.ExistsByCodigoImt(10);
        exists.Should().BeFalse();
    }

    [Test]
    public async Task FindById_ValidId_ReturnsEntity()
    {
        _dbContext.Equipos.Add(new Equipo { Id = 1, NumeroSerial = "E1", EstadoEliminado = false });
        await _dbContext.SaveChangesAsync();

        var result = await _repository.FindById(1);
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Test]
    public async Task Delete_ValidId_SoftDeletes()
    {
        _dbContext.Equipos.Add(new Equipo { Id = 1, NumeroSerial = "E1", EstadoEliminado = false });
        await _dbContext.SaveChangesAsync();

        var result = await _repository.Delete(1);
        result.IsSuccess.Should().BeTrue();

        var deleted = await _dbContext.Equipos.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == 1);
        deleted!.EstadoEliminado.Should().BeTrue();
    }

    [Test]
    public async Task GetByGrupo_ReturnsMappedList()
    {
        var grupo = new GrupoEquipo { Id = 5, Nombre = "G1" };
        var grupo2 = new GrupoEquipo { Id = 6, Nombre = "G2" };
        _dbContext.GruposEquipos.AddRange(grupo, grupo2);

        _dbContext.Equipos.Add(new Equipo { Id = 1, IdGrupoEquipo = 5, GrupoEquipo = grupo, NumeroSerial = "E1", EstadoEliminado = false });
        _dbContext.Equipos.Add(new Equipo { Id = 2, IdGrupoEquipo = 5, GrupoEquipo = grupo, NumeroSerial = "E2", EstadoEliminado = false });
        _dbContext.Equipos.Add(new Equipo { Id = 3, IdGrupoEquipo = 6, GrupoEquipo = grupo2, NumeroSerial = "E3", EstadoEliminado = false });
        await _dbContext.SaveChangesAsync();

        var list = await _repository.GetByGrupo(5);
        list.Should().HaveCount(2);
        list.Should().Contain(e => e.Id == 1);
        list.Should().Contain(e => e.Id == 2);
    }

    [Test]
    public async Task GetByGavetero_ReturnsMappedList()
    {
        var gavetero = new Gavetero { Id = 10 };
        var grupo = new GrupoEquipo { Id = 1, Nombre = "G1" };
        _dbContext.Gaveteros.Add(gavetero);
        _dbContext.GruposEquipos.Add(grupo);

        _dbContext.Equipos.Add(new Equipo { Id = 1, IdGavetero = 10, Gavetero = gavetero, IdGrupoEquipo = 1, GrupoEquipo = grupo, NumeroSerial = "E1", EstadoEliminado = false });
        await _dbContext.SaveChangesAsync();

        var list = await _repository.GetByGavetero(10);
        list.Should().HaveCount(1);
        list.First().Id.Should().Be(1);
    }

    [Test]
    public async Task GetHistorial_DuplicateDetails_ReturnsLoanOnce()
    {
        _dbContext.Usuarios.Add(
            new Usuario { Carnet = "12890061", Nombre = "Fernando", ApellidoPaterno = "Terrazas" }
        );
        _dbContext.Prestamos.Add(
            new Prestamo
            {
                Id = 264,
                Carnet = "12890061",
                FechaPrestamoEsperada = DateTime.UtcNow,
                FechaDevolucionEsperada = DateTime.UtcNow.AddHours(1),
                EstadoPrestamo = EstadoPrestamo.Activo,
            }
        );
        _dbContext.DetallesPrestamos.AddRange(
            new DetallePrestamo { Id = 1, IdPrestamo = 264, IdEquipo = 9, IdGrupoEquipo = 3 },
            new DetallePrestamo { Id = 2, IdPrestamo = 264, IdEquipo = 9, IdGrupoEquipo = 3 }
        );
        await _dbContext.SaveChangesAsync();

        var history = await _repository.GetHistorial(9);

        history.Should().ContainSingle();
        history.Single().IdPrestamo.Should().Be(264);
    }
}
