using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Componente;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace IMT_Reservas.Tests.Unit.Repositories;

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
