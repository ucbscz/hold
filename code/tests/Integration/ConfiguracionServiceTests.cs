using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Configuracion;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IMT_Reservas.Tests.Integration;

[TestFixture]
internal class ConfiguracionServiceTests
{
    private ApplicationDbContext _db = null!;
    private ConfiguracionService _service = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        IDistributedCache distributedCache = new MemoryDistributedCache(
            Options.Create(new MemoryDistributedCacheOptions())
        );
        var cache = new CacheRepository(
            distributedCache,
            NullLogger<CacheRepository>.Instance
        );
        var repository = new ConfiguracionRepository(_db, cache);
        var mapper = new ConfiguracionMapper();
        _service = new ConfiguracionService(repository, new ConfiguracionValidator(), mapper);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task UpdateConfiguracion_PersistsChangesAndInvalidatesCache()
    {
        var current = await _service.GetConfiguracion();
        current.MontoMinimoContrato += 25;
        current.HorarioFinMinutos = 17 * 60;
        current.NombreJefeCarrera = "Jefe actualizado";

        var result = await _service.UpdateConfiguracion(current);
        var reloaded = await _service.GetConfiguracion();

        result.IsSuccess.Should().BeTrue();
        reloaded.MontoMinimoContrato.Should().Be(current.MontoMinimoContrato);
        reloaded.HorarioFinMinutos.Should().Be(17 * 60);
        reloaded.NombreJefeCarrera.Should().Be("Jefe actualizado");
    }
}
