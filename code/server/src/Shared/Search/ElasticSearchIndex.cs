using Ardalis.Result;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using IMT_Reservas.Server.Application.Abstraction;

namespace IMT_Reservas.Server.Infrastructure.Config;

public sealed class ElasticSearchIndex<TDocument> : ISearchIndex<TDocument>
    where TDocument : class
{
    private static readonly string IndexName = typeof(TDocument).Name.ToLowerInvariant();
    private readonly ElasticsearchClient _client;

    public ElasticSearchIndex(ElasticsearchClient client) => _client = client;

    public bool Enabled => true;

    public async Task Index(int id, TDocument document) =>
        await _client.IndexAsync(document, descriptor => descriptor.Index(IndexName).Id(id));

    public async Task IndexMany(
        IReadOnlyCollection<TDocument> documents,
        Func<TDocument, int> idSelector
    )
    {
        if (documents.Count == 0)
            return;

        await _client.BulkAsync(bulk =>
            bulk.Index(IndexName)
                .IndexMany(documents, (operation, document) => operation.Id(idSelector(document)))
        );
    }

    public async Task Remove(int id) => await _client.DeleteAsync(IndexName, id);

    public async Task<Result<IReadOnlyCollection<int>>> Search(SearchQuery query)
    {
        var response = await _client.SearchAsync<TDocument>(search =>
            search.Index(IndexName).Size(query.Size).Query(root => BuildTextQuery(root, query))
        );

        if (!response.IsValidResponse)
            return Result<IReadOnlyCollection<int>>.Error(response.DebugInformation);

        IReadOnlyCollection<int> ids = response
            .Hits.Select(hit => int.TryParse(hit.Id, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToList();

        return Result<IReadOnlyCollection<int>>.Success(ids);
    }

    private static void BuildTextQuery(QueryDescriptor<TDocument> clause, SearchQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.Term))
        {
            clause.MatchAll(_ => { });
            return;
        }

        clause.MultiMatch(match =>
            match
                .Query(query.Term)
                .Fields(query.Fields.ToArray())
                .Type(TextQueryType.BestFields)
                .Fuzziness(new Fuzziness("AUTO"))
        );
    }
}
