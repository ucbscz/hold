namespace UCB_Hold.Server.Shared.Caching;

public sealed class RedisSettings
{
    public const string SectionName = "Redis";

    public bool? Enabled { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string InstanceName { get; set; } = string.Empty;
    public int ConnectTimeoutMilliseconds { get; set; } = 5000;
    public int SyncTimeoutMilliseconds { get; set; } = 1000;
    public int ConnectRetry { get; set; } = 3;
}
