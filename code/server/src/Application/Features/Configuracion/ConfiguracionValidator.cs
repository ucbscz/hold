using FluentValidation;

namespace IMT_Reservas.Server.Application.Features.Configuracion;

public class ConfiguracionValidator : AbstractValidator<ConfiguracionDto>
{
    public ConfiguracionValidator()
    {
        RuleFor(c => c.MontoMinimoContrato)
            .GreaterThanOrEqualTo(0).WithMessage("El monto mínimo no puede ser negativo.");

        RuleFor(c => c.HorarioInicioMinutos)
            .GreaterThanOrEqualTo(0).WithMessage("El horario de inicio debe ser válido.")
            .LessThan(c => c.HorarioFinMinutos).WithMessage("El horario de inicio debe ser menor al horario de fin.");

        RuleFor(c => c.HorarioFinMinutos)
            .LessThanOrEqualTo(1440).WithMessage("El horario de fin no puede exceder las 24 horas.");

        RuleFor(c => c.NombreJefeCarrera)
            .NotEmpty().WithMessage("El nombre del jefe de carrera es obligatorio.")
            .MaximumLength(255).WithMessage("El nombre no puede exceder los 255 caracteres.");

        RuleFor(c => c.FirmaJefeCarreraBase64)
            .NotEmpty().WithMessage("La firma es obligatoria.");

        RuleFor(c => c.TiempoMinimoReservaMinutos)
            .GreaterThan(0).WithMessage("El tiempo mínimo de reserva debe ser mayor a cero.");

        RuleFor(c => c.TiempoRecordatorioPrevioMinutos)
            .GreaterThanOrEqualTo(0).WithMessage("El tiempo de recordatorio no puede ser negativo.");

        RuleFor(c => c.MinutosGraciaAtraso)
            .GreaterThanOrEqualTo(0).WithMessage("Los minutos de gracia no pueden ser negativos.");
    }
}
