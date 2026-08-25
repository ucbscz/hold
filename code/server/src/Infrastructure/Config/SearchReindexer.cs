using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Application.Features.GrupoEquipo;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Infrastructure.Config;

public sealed class SearchReindexer : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SearchReindexer> _logger;

    public SearchReindexer(IServiceScopeFactory scopeFactory, ILogger<SearchReindexer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<GrupoEquipoRepository>();
            var index = scope.ServiceProvider.GetRequiredService<ISearchIndex<GrupoEquipoDto>>();
            var groups = await repository.Search();

            await index.IndexMany(groups, group => group.Id ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Search reindex skipped on startup. The database or search engine may not be ready yet. The index will populate as data is added."
            );
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
