using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Modules.Prestamos.Domain;
using UCB_Hold.Server.Modules.Prestamos.Infrastructure;
using UCB_Hold.Server.Modules.Usuarios.Domain;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Tests.Unit.Modules.Prestamos;

[TestFixture]
public class HistorialPrestamosEquipoTests
{
    private ApplicationDbContext _dbContext = null!;

    [SetUp]
    public void SetUp() =>
        _dbContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options
        );

    [TearDown]
    public void TearDown() => _dbContext.Dispose();

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

        var history = await new HistorialPrestamosEquipo(_dbContext).GetHistorial(9);

        history.Should().ContainSingle();
        history.Single().IdPrestamo.Should().Be(264);
    }
}
