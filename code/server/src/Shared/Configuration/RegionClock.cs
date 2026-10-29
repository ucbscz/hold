using Microsoft.Extensions.Options;

namespace IMT_Reservas.Server.Shared.Configuration;

public sealed class RegionClock
{
    private readonly TimeZoneInfo _timeZone;

    public RegionClock(IOptions<RegionSettings> options)
    {
        var settings = options.Value;
        Name = settings.Name;
        _timeZone = Resolve(settings);
    }

    public string Name { get; }

    public DateTime ToLocal(DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified)
            return value;

        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
        return TimeZoneInfo.ConvertTimeFromUtc(utc, _timeZone);
    }

    private static TimeZoneInfo Resolve(RegionSettings settings)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(
                settings.TimeZoneId,
                TimeSpan.FromHours(settings.UtcOffsetHours),
                settings.Name,
                settings.Name
            );
        }
    }
}
