using FluentValidation;
using IMT_Reservas.Server.Application.Features.Contrato;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Application.Features.Prestamo;

public class PrestamoValidator : AbstractValidator<PrestamoDto>
{
    public PrestamoValidator(
        ApplicationDbContext dbContext,
        ConfiguracionRepository configRepository
    )
    {
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

                var config = await configRepository.GetConfiguracion();
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

                var config = await configRepository.GetConfiguracion();
                return HorarioReserva.EsValido(
                    p.FechaPrestamoEsperada.Value,
                    p.FechaDevolucionEsperada.Value,
                    config.HorarioInicioMinutos,
                    config.HorarioFinMinutos
                );
            })
            .WithMessage(HorarioReserva.Mensaje);
    }
}
