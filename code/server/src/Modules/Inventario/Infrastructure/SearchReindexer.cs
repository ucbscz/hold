using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Shared.Kernel;
using UCB_Hold.Server.Shared.Search;

namespace UCB_Hold.Server.Modules.Inventario.Infrastructure;

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
            _logger.SearchReindexSkipped(ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
