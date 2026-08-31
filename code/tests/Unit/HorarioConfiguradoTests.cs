using FluentAssertions;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Tests.Unit;

[TestFixture]
public class HorarioConfiguradoTests
{
    private static ConfiguracionSistema Config() => new() {
        HorarioInicioMinutos = 480, HorarioFinMinutos = 1080, TiempoMinimoReservaMinutos = 30
    };

    [Test]
    public void WeeklySchedule_ReplacesDefaultHours()
    {
        var config = Config();
        config.Horarios.Add(new() { DiaSemana = 1, InicioMinutos = 600, FinMinutos = 960 });
        HorarioReserva.EsValido(new(2026, 8, 31, 9, 0, 0), new(2026, 8, 31, 10, 0, 0), config).Should().BeFalse();
        HorarioReserva.EsValido(new(2026, 8, 31, 15, 30, 0), new(2026, 8, 31, 16, 0, 0), config).Should().BeTrue();
        HorarioReserva.EsValido(new(2026, 8, 31, 15, 31, 0), new(2026, 9, 1, 8, 30, 0), config).Should().BeFalse();
    }

    [Test]
    public void DateOverride_WinsOverWeeklySchedule()
    {
        var config = Config();
        config.Horarios.Add(new() { DiaSemana = 1, InicioMinutos = 480, FinMinutos = 1080 });
        config.Horarios.Add(new() { Fecha = new DateOnly(2026, 8, 31), Abierto = false });
        HorarioReserva.EsValido(new(2026, 8, 31, 9, 0, 0), new(2026, 8, 31, 9, 30, 0), config).Should().BeFalse();
    }

    [Test]
    public void Sunday_DefaultClosedButCanBeExplicitlyOpened()
    {
        var config = Config();
        var inicio = new DateTime(2026, 8, 30, 13, 0, 0, DateTimeKind.Utc);
        HorarioReserva.EsValido(inicio, inicio.AddMinutes(30), config).Should().BeFalse();
        config.Horarios.Add(new() { DiaSemana = 0, InicioMinutos = 540, FinMinutos = 720 });
        HorarioReserva.EsValido(inicio, inicio.AddMinutes(30), config).Should().BeTrue();
    }

    [Test]
    public void InternalDate_UsesBoliviaRatherThanUtcMidnight()
    {
        HorarioReserva.MismoDia(new(2026, 8, 31, 23, 0, 0, DateTimeKind.Utc), new(2026, 9, 1, 1, 0, 0, DateTimeKind.Utc)).Should().BeTrue();
    }
}
