using Ardalis.Result;

namespace IMT_Reservas.Server.Application.Abstraction;

public interface ISearchIndex<TDocument>
    where TDocument : class
{
    bool Enabled { get; }
    Task Index(int id, TDocument document);
    Task IndexMany(IReadOnlyCollection<TDocument> documents, Func<TDocument, int> idSelector);
    Task Remove(int id);
    Task<Result<IReadOnlyCollection<int>>> Search(SearchQuery query);
}
