using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using IMT_Reservas.Server.Modules.Inventario.Application;
using IMT_Reservas.Server.Modules.Inventario.Domain;
using IMT_Reservas.Server.Modules.Inventario.Infrastructure;
using IMT_Reservas.Server.Shared.Persistence;

namespace IMT_Reservas.Tests.Unit.Modules.Inventario;

[TestFixture]
public class ComponenteRepositoryTests
{
    private ApplicationDbContext _dbContext = null!;
    private ComponenteRepository _repository = null!;
    private ComponenteMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mapper = new ComponenteMapper();
        _repository = new ComponenteRepository(_dbContext, _mapper);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task Get_ExistingId_ReturnsDto()
    {
        var entity = new Componente { Id = 1, EstadoEliminado = false };
        _dbContext.Componentes.Add(entity);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.Get(1);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task GetAll_ReturnsList()
    {
        _dbContext.Componentes.Add(new Componente { Id = 1, EstadoEliminado = false });
        _dbContext.Componentes.Add(new Componente { Id = 2, EstadoEliminado = false });
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetAll();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Test]
    public async Task Create_ValidEntity_ReturnsCreated()
    {
        var entity = new Componente { Id = 1, EstadoEliminado = false };
        var result = await _repository.Create(entity);

        result.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Delete_ExistingId_ReturnsSuccess()
    {
        var entity = new Componente { Id = 1, EstadoEliminado = false };
        _dbContext.Componentes.Add(entity);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.Delete(1);
        result.IsSuccess.Should().BeTrue();
    }
}
