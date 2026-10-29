using Elastic.Clients.Elasticsearch;

namespace IMT_Reservas.Server.Shared.Search;

public static class SearchServiceRegistration
{
    public static IServiceCollection AddSearch(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        if (!ElasticsearchSettings.IsConfigured(configuration))
            return services.AddSingleton(typeof(ISearchIndex<>), typeof(NullSearchIndex<>));

        var clientSettings = new ElasticsearchClientSettings(
            new Uri(ElasticsearchSettings.Read(configuration).Uri)
        ).DefaultFieldNameInferrer(name => name);
        services.AddSingleton(new ElasticsearchClient(clientSettings));
        return services.AddSingleton(typeof(ISearchIndex<>), typeof(ElasticSearchIndex<>));
    }
}
