using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Equipo;
using IMT_Reservas.Server.Application.Features.GrupoEquipo;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace IMT_Reservas.Tests.Unit.Repositories;

[TestFixture]
public class GrupoEquipoRepositoryTests
{
    private ApplicationDbContext _dbContext = null!;
    private GrupoEquipoRepository _repository = null!;
    private GrupoEquipoMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mapper = new GrupoEquipoMapper();
        var equipoMapper = new EquipoMapper();
        var equipoRepository = new EquipoRepository(_dbContext, equipoMapper);
        _repository = new GrupoEquipoRepository(_dbContext, _mapper, equipoRepository);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task Search_ByName_ReturnsMatches()
    {
        var categoria = new Categoria { Id = 10, Nombre = "Cat" };
        _dbContext.Categorias.Add(categoria);
        _dbContext.GruposEquipos.Add(new GrupoEquipo { Id = 1, Nombre = "Osciloscopio", Modelo = "", Marca = "", Categoria = categoria, EstadoEliminado = false });
        _dbContext.GruposEquipos.Add(new GrupoEquipo { Id = 2, Nombre = "Multimetro", Modelo = "", Marca = "", Categoria = categoria, EstadoEliminado = false });
        await _dbContext.SaveChangesAsync();

        var result = await _repository.Search("Osciloscopio");

        result.Should().HaveCount(1);
        result.First().Id.Should().Be(1);
    }

    [Test]
    public async Task Search_ByCategoria_ReturnsMatches()
    {
        var categoria = new Categoria { Id = 1, Nombre = "Electrónica", EstadoEliminado = false };
        _dbContext.Categorias.Add(categoria);
        _dbContext.GruposEquipos.Add(new GrupoEquipo { Id = 1, Nombre = "Osciloscopio", Modelo = "", Marca = "", Categoria = categoria, EstadoEliminado = false });
        _dbContext.GruposEquipos.Add(new GrupoEquipo { Id = 2, Nombre = "Cautin", Modelo = "", Marca = "", Categoria = categoria, EstadoEliminado = false });
        await _dbContext.SaveChangesAsync();

        var result = await _repository.Search(null, "Electrónica");

        result.Should().HaveCount(2);
        result.First().Id.Should().Be(1);
    }

    [Test]
    public async Task Search_Empty_ReturnsAll()
    {
        var categoria = new Categoria { Id = 20, Nombre = "Cat2" };
        _dbContext.Categorias.Add(categoria);
        _dbContext.GruposEquipos.Add(new GrupoEquipo { Id = 1, Nombre = "G1", Modelo = "", Marca = "", Categoria = categoria, EstadoEliminado = false });
        _dbContext.GruposEquipos.Add(new GrupoEquipo { Id = 2, Nombre = "G2", Modelo = "", Marca = "", Categoria = categoria, EstadoEliminado = false });
        await _dbContext.SaveChangesAsync();

        var result = await _repository.Search();

        result.Should().HaveCount(2);
    }

    [Test]
    public async Task FindCategoriaIdByNombre_Valid_ReturnsId()
    {
        _dbContext.Categorias.Add(new Categoria { Id = 5, Nombre = "Sensores", EstadoEliminado = false });
        await _dbContext.SaveChangesAsync();

        var id = await _repository.FindCategoriaIdByNombre("Sensores");

        id.Should().Be(5);
    }

    [Test]
    public async Task FindCategoriaIdByNombre_Invalid_ReturnsNull()
    {
        var id = await _repository.FindCategoriaIdByNombre("Invalid");

        id.Should().BeNull();
    }
}
