using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace IMT_Reservas.Tests.Helpers;

internal abstract class ServiceTest<TService>
{
    private string _dbName = string.Empty;

    protected ApplicationDbContext Db { get; private set; } = null!;
    protected TService Sut { get; private set; } = default!;
    protected CacheRepository Cache { get; private set; } = null!;

    protected abstract TService CreateService(ApplicationDbContext db);

    [SetUp]
    public void SetUp()
    {
        _dbName = Guid.NewGuid().ToString();
        var memCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        Cache = new CacheRepository(memCache, NullLogger<CacheRepository>.Instance);
        Db = NewDbContext();
        Sut = CreateService(Db);
    }

    protected ApplicationDbContext NewDbContext()
        => new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options);

    protected TService NewSut() => CreateService(NewDbContext());

    [TearDown]
    public void TearDown() => Db.Dispose();
}
