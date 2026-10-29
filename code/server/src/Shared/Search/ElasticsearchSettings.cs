namespace UCB_Hold.Server.Shared.Search;

public sealed class ElasticsearchSettings
{
    public const string SectionName = "Elasticsearch";

    public string Uri { get; set; } = string.Empty;

    public static ElasticsearchSettings Read(IConfiguration configuration) =>
        configuration.GetSection(SectionName).Get<ElasticsearchSettings>() ?? new ElasticsearchSettings();

    public static bool IsConfigured(IConfiguration configuration) =>
        !string.IsNullOrWhiteSpace(Read(configuration).Uri);
}
