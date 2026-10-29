using Elastic.Clients.Elasticsearch;
using IMT_Reservas.Server.Application.Abstraction;

namespace IMT_Reservas.Server.Infrastructure.Config;

public static class SearchServiceRegistration
{
    public static IServiceCollection AddSearch(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var settings =
            configuration.GetSection(ElasticsearchSettings.SectionName).Get<ElasticsearchSettings>()
            ?? new ElasticsearchSettings();

        if (string.IsNullOrWhiteSpace(settings.Uri))
            return services.AddSingleton(typeof(ISearchIndex<>), typeof(NullSearchIndex<>));

        var clientSettings = new ElasticsearchClientSettings(
            new Uri(settings.Uri)
        ).DefaultFieldNameInferrer(name => name);
        services.AddSingleton(new ElasticsearchClient(clientSettings));
        services.AddSingleton(typeof(ISearchIndex<>), typeof(ElasticSearchIndex<>));
        return services.AddHostedService<SearchReindexer>();
    }
}
