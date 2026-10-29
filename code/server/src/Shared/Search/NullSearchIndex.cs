using Ardalis.Result;
using IMT_Reservas.Server.Application.Abstraction;

namespace IMT_Reservas.Server.Infrastructure.Config;

public sealed class NullSearchIndex<TDocument> : ISearchIndex<TDocument>
    where TDocument : class
{
    public bool Enabled => false;

    public Task Index(int id, TDocument document) => Task.CompletedTask;

    public Task IndexMany(
        IReadOnlyCollection<TDocument> documents,
        Func<TDocument, int> idSelector
    ) => Task.CompletedTask;

    public Task Remove(int id) => Task.CompletedTask;

    public Task<Result<IReadOnlyCollection<int>>> Search(SearchQuery query) =>
        Task.FromResult(Result<IReadOnlyCollection<int>>.Success([]));
}
