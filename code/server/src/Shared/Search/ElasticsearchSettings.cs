namespace IMT_Reservas.Server.Infrastructure.Config;

public sealed class ElasticsearchSettings
{
    public const string SectionName = "Elasticsearch";

    public string Uri { get; set; } = string.Empty;
}
