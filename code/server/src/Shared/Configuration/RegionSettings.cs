namespace UCB_Hold.Server.Shared.Configuration;

public sealed class RegionSettings
{
    public const string SectionName = "Region";

    public string Name { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = string.Empty;

    public double UtcOffsetHours { get; set; }
}
