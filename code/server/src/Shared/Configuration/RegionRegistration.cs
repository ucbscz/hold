namespace UCB_Hold.Server.Shared.Configuration;

public static class RegionRegistration
{
    public static IServiceCollection AddRegion(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var section = configuration.GetSection(RegionSettings.SectionName);
        var settings = section.Get<RegionSettings>() ?? new RegionSettings();
        if (string.IsNullOrWhiteSpace(settings.Name) || string.IsNullOrWhiteSpace(settings.TimeZoneId))
            throw new InvalidOperationException(
                $"{RegionSettings.SectionName}:{nameof(RegionSettings.Name)} and {nameof(RegionSettings.TimeZoneId)} are required."
            );

        services.Configure<RegionSettings>(section);
        return services.AddSingleton<RegionClock>();
    }
}
