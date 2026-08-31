namespace IMT_Reservas.Server.Application.Features.Prestamo;

public static class HorarioReserva
{
    public const int HoraApertura = 8;
    public const int HoraCierre = 18;
    public const string Mensaje =
        "Selecciona una fecha y hora dentro del horario de atención autorizado (hora de Bolivia)";

    private static readonly TimeZoneInfo BoliviaTimeZone = ResolveBoliviaTimeZone();

    public static bool EsValido(DateTime inicio, DateTime fin, Core.Entities.ConfiguracionSistema config) =>
        fin > inicio && DentroHorario(EnBolivia(inicio), config, false)
            && DentroHorario(EnBolivia(fin), config, true);

    private static bool DentroHorario(DateTime fecha, Core.Entities.ConfiguracionSistema config, bool devolucion)
    {
        var horario = config.Horarios.FirstOrDefault(h => h.Fecha == DateOnly.FromDateTime(fecha))
            ?? config.Horarios.FirstOrDefault(h => h.Fecha == null && h.DiaSemana == (int)fecha.DayOfWeek);
        var abierto = horario?.Abierto ?? fecha.DayOfWeek != DayOfWeek.Sunday;
        var inicio = horario?.InicioMinutos ?? config.HorarioInicioMinutos;
        var fin = horario?.FinMinutos ?? config.HorarioFinMinutos;
        var minutos = fecha.TimeOfDay.TotalMinutes;
        return abierto && minutos >= inicio && (devolucion ? minutos <= fin : minutos <= fin - config.TiempoMinimoReservaMinutos);
    }

    public static bool MismoDia(DateTime inicio, DateTime fin) => EnBolivia(inicio).Date == EnBolivia(fin).Date;

    public static bool EsValido(DateTime inicio, DateTime fin, int aperturaMinutos, int cierreMinutos)
    {
        var inicioLocal = EnBolivia(inicio);
        var finLocal = EnBolivia(fin);
        var apertura = TimeSpan.FromMinutes(aperturaMinutos);
        var cierre = TimeSpan.FromMinutes(cierreMinutos);

        return inicioLocal.DayOfWeek != DayOfWeek.Sunday
            && finLocal.DayOfWeek != DayOfWeek.Sunday
            && inicioLocal.TimeOfDay >= apertura
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
