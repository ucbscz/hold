using FluentValidation;
using Microsoft.EntityFrameworkCore;
using IMT_Reservas.Server.Modules.Configuracion.Infrastructure;
using IMT_Reservas.Server.Modules.Prestamos.Domain;
using IMT_Reservas.Server.Shared.Persistence;

namespace IMT_Reservas.Server.Modules.Prestamos.Application;

public class PrestamoValidator : AbstractValidator<PrestamoDto>
{
    private readonly HorarioReserva _horario;
    public PrestamoValidator(
        ApplicationDbContext dbContext,
        HorarioReserva horario,
        ConfiguracionRepository configRepository
    )
    {
        _horario = horario;
        RuleFor(p => p.DestinoPrestamo).Must(DestinoUso.EsValido)
            .WithMessage("Selecciona uso interno o externo");
        RuleFor(p => p).Must(p => p.DestinoPrestamo == DestinoUso.Casa
            || !p.FechaPrestamoEsperada.HasValue || !p.FechaDevolucionEsperada.HasValue
            || _horario.MismoDia(p.FechaPrestamoEsperada.Value, p.FechaDevolucionEsperada.Value))
            .WithMessage("El uso interno en clases o laboratorio requiere devolución el mismo día");
        RuleFor(p => p.CarnetUsuario)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Carnet de usuario requerido")
            .MustAsync(
                async (carnet, cancellationToken) =>
                    await dbContext.Usuarios.AnyAsync(
                        u => u.Carnet == carnet && !u.EstadoEliminado,
                        cancellationToken
                    )
            )
            .WithMessage("Usuario no existe o está inactivo");

        RuleFor(p => p.GrupoEquipoId)
            .NotEmpty()
            .WithMessage("Debe seleccionar al menos un equipo")
            .Must(groups => groups == null || groups.Count <= 100)
            .WithMessage("No se pueden reservar más de 100 unidades por solicitud");

        RuleForEach(p => p.GrupoEquipoId)
            .GreaterThan(0)
            .WithMessage("El identificador del equipo no es válido");

        RuleFor(p => p.Contrato)
            .MaximumLength(ContractHtmlProcessor.MaxHtmlLength)
            .WithMessage("El contrato supera el tamaño máximo permitido");

        RuleFor(p => p.FechaPrestamoEsperada)
            .NotNull()
            .WithMessage("Fecha préstamo esperada requerida")
            .Must(date => !date.HasValue || date.Value >= DateTime.UtcNow)
            .WithMessage("La fecha y hora de inicio no puede estar en el pasado")
            .Must(date => !date.HasValue || date.Value <= DateTime.UtcNow.AddYears(1))
            .WithMessage("La fecha de inicio no puede superar un año desde la fecha actual");

        RuleFor(p => p.FechaDevolucionEsperada)
            .NotNull()
            .WithMessage("Fecha devolución esperada requerida");

        RuleFor(p => p)
            .Must(p =>
                !p.FechaPrestamoEsperada.HasValue
                || !p.FechaDevolucionEsperada.HasValue
                || p.FechaDevolucionEsperada.Value > p.FechaPrestamoEsperada.Value
            )
            .WithMessage("La fecha y hora de devolución debe ser posterior al inicio del préstamo");

        RuleFor(p => p)
            .MustAsync(async (p, cancellationToken) =>
            {
                if (!p.FechaPrestamoEsperada.HasValue || !p.FechaDevolucionEsperada.HasValue) return true;

                var config = await configRepository.GetConfiguracion(cancellationToken);
                var duracion = p.FechaDevolucionEsperada.Value - p.FechaPrestamoEsperada.Value;

                if (duracion < TimeSpan.FromMinutes(config.TiempoMinimoReservaMinutos))
                    return false;

                return true;
            })
            .WithMessage(p => "El préstamo no cumple la duración mínima requerida");

        RuleFor(p => p)
            .MustAsync(async (p, cancellationToken) =>
            {
                if (!p.FechaPrestamoEsperada.HasValue || !p.FechaDevolucionEsperada.HasValue) return true;

                var config = await configRepository.GetConfiguracion(cancellationToken);
                return _horario.EsValido(
                    p.FechaPrestamoEsperada.Value,
                    p.FechaDevolucionEsperada.Value,
                    config
                );
            })
            .WithMessage(_horario.Mensaje);
    }
}
