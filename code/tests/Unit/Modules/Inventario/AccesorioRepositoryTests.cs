using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Accesorio;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace IMT_Reservas.Tests.Unit.Repositories;

[TestFixture]
public class AccesorioRepositoryTests
{
    private ApplicationDbContext _dbContext = null!;
    private AccesorioRepository _repository = null!;
    private AccesorioMapper _mapper = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _mapper = new AccesorioMapper();
        _repository = new AccesorioRepository(_dbContext, _mapper);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task Create_ValidEntity_ReturnsCreated()
    {
        _dbContext.Equipos.Add(new Equipo { Id = 10, CodigoImt = 123, Descripcion = "Eq1", NumeroSerial = "1", EstadoEliminado = false });
        await _dbContext.SaveChangesAsync();

        var entity = new Accesorio { Nombre = "Acc1", IdEquipo = 10, Modelo = "", EstadoEliminado = false };
        var result = await _repository.Create(entity);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Nombre.Should().Be("Acc1");
    }

    [Test]
    public async Task Create_DeletedEntity_PersistsItAsActive()
    {
        _dbContext.Equipos.Add(
            new Equipo
            {
                Id = 10,
                CodigoImt = 123,
                Descripcion = "Eq1",
                NumeroSerial = "1",
                EstadoEliminado = false,
            }
        );
        await _dbContext.SaveChangesAsync();

        var entity = new Accesorio
        {
            Nombre = "Acc1",
            Modelo = "",
            IdEquipo = 10,
            EstadoEliminado = true,
        };

        var result = await _repository.Create(entity);

        result.IsSuccess.Should().BeTrue();
        var stored = await _dbContext.Accesorios.SingleAsync();
        stored.EstadoEliminado.Should().BeFalse();
    }

    [Test]
    public async Task Update_ValidEntity_ReturnsSuccess()
    {
        _dbContext.Equipos.Add(new Equipo { Id = 10, CodigoImt = 123, Descripcion = "Eq1", NumeroSerial = "1", EstadoEliminado = false });
        var entity = new Accesorio { Nombre = "Acc1", IdEquipo = 10, Modelo = "", EstadoEliminado = false };
        _dbContext.Accesorios.Add(entity);
        await _dbContext.SaveChangesAsync();

        entity.Nombre = "Acc1-Updated";
        var result = await _repository.Update(entity);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Nombre.Should().Be("Acc1-Updated");
    }

    [Test]
    public async Task Update_NonExistingEntity_ReturnsNotFound()
    {
        var entity = new Accesorio
        {
            Id = 999,
            Nombre = "Acc1",
            Modelo = "",
            EstadoEliminado = false,
        };

        var result = await _repository.Update(entity);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
        (await _dbContext.Accesorios.IgnoreQueryFilters().CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task Update_SoftDeletedEntity_DoesNotReactivateIt()
    {
        var entity = new Accesorio
        {
            Nombre = "Acc1",
            Modelo = "",
            EstadoEliminado = true,
        };
        _dbContext.Accesorios.Add(entity);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        entity.Nombre = "Acc1-Updated";
        entity.EstadoEliminado = false;
        var result = await _repository.Update(entity);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
        var stored = await _dbContext.Accesorios.IgnoreQueryFilters().SingleAsync();
        stored.Nombre.Should().Be("Acc1");
        stored.EstadoEliminado.Should().BeTrue();
    }

    [Test]
    public async Task Delete_ExistingId_ReturnsSuccess()
    {
        var entity = new Accesorio { Nombre = "Acc1", EstadoEliminado = false };
        _dbContext.Accesorios.Add(entity);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.Delete(entity.Id);

        result.IsSuccess.Should().BeTrue();
        var inDb = await _dbContext.Accesorios.FindAsync(entity.Id);
        inDb.Should().NotBeNull();
        inDb!.EstadoEliminado.Should().BeTrue();
    }

    [Test]
    public async Task Delete_NonExistingId_ReturnsNotFound()
    {
        var result = await _repository.Delete(999);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Test]
    public async Task Get_ExistingId_ReturnsDto()
    {
        _dbContext.Equipos.Add(new Equipo { Id = 10, CodigoImt = 123, Descripcion = "Eq1", NumeroSerial = "1", EstadoEliminado = false });
        var entity = new Accesorio { Nombre = "Acc1", IdEquipo = 10, Modelo = "", EstadoEliminado = false };
        _dbContext.Accesorios.Add(entity);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.Get(entity.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Nombre.Should().Be("Acc1");
    }

    [Test]
    public async Task Get_NonExistingId_ReturnsNotFound()
    {
        var result = await _repository.Get(999);

        result.Status.Should().Be(Ardalis.Result.ResultStatus.NotFound);
    }

    [Test]
    public async Task GetAll_ReturnsListOfDtos()
    {
        _dbContext.Equipos.Add(new Equipo { Id = 10, CodigoImt = 123, Descripcion = "Eq1", NumeroSerial = "1", EstadoEliminado = false });
        _dbContext.Accesorios.Add(new Accesorio { Nombre = "Acc1", IdEquipo = 10, Modelo = "", EstadoEliminado = false });
        _dbContext.Accesorios.Add(new Accesorio { Nombre = "Acc2", IdEquipo = 10, Modelo = "", EstadoEliminado = false });
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetAll();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
