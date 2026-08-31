using FluentValidation;

namespace IMT_Reservas.Server.Application.Features.Configuracion;

public class ConfiguracionValidator : AbstractValidator<ConfiguracionDto>
{
    public ConfiguracionValidator()
    {
        RuleFor(c => c.Horarios).NotNull().Must(h => h != null && h.Count <= 373 && h.All(d => d != null)
            && h.Select(d => d.Fecha?.ToString("yyyy-MM-dd") ?? d.DiaSemana.ToString()).Distinct().Count() == h.Count)
            .WithMessage("No repitas días u horarios y limita las excepciones a un año.");
        RuleForEach(c => c.Horarios).Must((config, h) => h != null && h.DiaSemana is >= 0 and <= 6
            && h.InicioMinutos is >= 0 and < 1440 && h.FinMinutos is >= 0 and < 1440
            && (!h.Abierto || h.FinMinutos - h.InicioMinutos >= Math.Max(30, config.TiempoMinimoReservaMinutos)))
            .WithMessage("Cada día abierto debe permitir la duración mínima de reserva configurada.");
        RuleFor(c => c.MontoMinimoContrato)
            .GreaterThanOrEqualTo(0).WithMessage("El monto mínimo no puede ser negativo.");

        RuleFor(c => c.HorarioInicioMinutos)
            .GreaterThanOrEqualTo(0).WithMessage("El horario de inicio debe ser válido.")
            .LessThan(c => c.HorarioFinMinutos).WithMessage("El horario de inicio debe ser menor al horario de fin.");

        RuleFor(c => c.HorarioFinMinutos)
            .InclusiveBetween(0, 1439).WithMessage("El horario de fin debe estar entre 00:00 y 23:59.");

        RuleFor(c => c).Must(c => (long)c.HorarioFinMinutos - c.HorarioInicioMinutos >= c.TiempoMinimoReservaMinutos)
            .WithMessage("El horario de atención debe permitir la duración mínima de reserva.");

        RuleFor(c => c.NombreJefeCarrera)
            .NotEmpty().WithMessage("El nombre del jefe de carrera es obligatorio.")
            .MaximumLength(255).WithMessage("El nombre no puede exceder los 255 caracteres.");

        RuleFor(c => c.FirmaJefeCarreraBase64)
            .NotEmpty().WithMessage("La firma es obligatoria.");

        RuleFor(c => c.CarnetJefeCarrera).MaximumLength(20);

        RuleFor(c => c.TiempoMinimoReservaMinutos)
            .GreaterThanOrEqualTo(30).WithMessage("La duración mínima debe ser de al menos 30 minutos.");

        RuleFor(c => c.TiempoRecordatorioPrevioMinutos)
            .GreaterThanOrEqualTo(0).WithMessage("El tiempo de recordatorio no puede ser negativo.");

        RuleFor(c => c.MinutosGraciaAtraso)
            .GreaterThanOrEqualTo(0).WithMessage("Los minutos de gracia no pueden ser negativos.");
    }
}
