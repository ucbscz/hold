namespace UCB_Hold.Server.Shared.Jobs;

public sealed class HangfireSettings
{
    public const string SectionName = "Hangfire";
    public const string DefaultDashboardPath = "/hangfire";

    public bool? Enabled { get; set; }
    public string DashboardPath { get; set; } = DefaultDashboardPath;
}
