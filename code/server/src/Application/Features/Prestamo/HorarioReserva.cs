namespace IMT_Reservas.Server.Application.Features.Prestamo;

public static class HorarioReserva
{
    public const int HoraApertura = 8;
    public const int HoraCierre = 18;
    public const string Mensaje =
        "El horario de atención para reservas es de 08:00 a 18:00 (hora de Bolivia)";

    private static readonly TimeZoneInfo BoliviaTimeZone = ResolveBoliviaTimeZone();

    public static bool EsValido(DateTime inicio, DateTime fin)
    {
        var inicioLocal = EnBolivia(inicio);
        var finLocal = EnBolivia(fin);
        var apertura = TimeSpan.FromHours(HoraApertura);
        var cierre = TimeSpan.FromHours(HoraCierre);

        return inicioLocal.TimeOfDay >= apertura
            && inicioLocal.TimeOfDay < cierre
            && finLocal.TimeOfDay >= apertura
            && finLocal.TimeOfDay <= cierre;
    }

    private static DateTime EnBolivia(DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified)
            return value;

        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => value,
        };

        return TimeZoneInfo.ConvertTimeFromUtc(utc, BoliviaTimeZone);
    }

    private static TimeZoneInfo ResolveBoliviaTimeZone()
    {
        foreach (var id in new[] { "America/La_Paz", "SA Western Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "Bolivia",
            TimeSpan.FromHours(-4),
            "Bolivia",
            "Bolivia"
        );
    }
}
