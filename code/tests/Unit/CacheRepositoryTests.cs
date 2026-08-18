using Ardalis.Result;
using FluentAssertions;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
internal class CacheRepositoryTests
{
    [Test]
    public async Task Get_WhenCacheFails_ReturnsNotFound()
    {
        var cache = new Mock<IDistributedCache>();
        cache
            .Setup(instance => instance.GetAsync("usuario:1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache unavailable"));
        var repository = CreateRepository(cache.Object);

        var result = await repository.Get<string>("usuario:1");

        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Test]
    public async Task Remove_WhenCacheFails_ReturnsErrorWithoutThrowing()
    {
        var cache = new Mock<IDistributedCache>();
        cache
            .Setup(instance => instance.RemoveAsync("usuario:1", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache unavailable"));
        var repository = CreateRepository(cache.Object);

        var result = await repository.Remove("usuario:1");

        result.Status.Should().Be(ResultStatus.Error);
    }

    private static CacheRepository CreateRepository(IDistributedCache cache) =>
        new(cache, NullLogger<CacheRepository>.Instance);
}
