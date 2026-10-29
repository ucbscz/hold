using FluentValidation;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Application.Features.Mantenimiento;

public class MantenimientoValidator : AbstractValidator<MantenimientoDto>
{
    public MantenimientoValidator(ApplicationDbContext dbContext)
    {
        RuleFor(m => m.IdEmpresa)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .GreaterThan(0)
            .WithMessage("IdEmpresa requerido")
            .MustAsync(
                async (id, cancellationToken) =>
                    await dbContext.EmpresasMantenimiento.AnyAsync(
                        e => e.Id == id && !e.EstadoEliminado,
                        cancellationToken
                    )
            )
            .WithMessage("Empresa mantenimiento no existe");

        RuleFor(m => m.FechaMantenimiento).NotNull().WithMessage("Fecha mantenimiento requerida");

        RuleFor(m => m.FechaFinalMantenimiento).NotNull().WithMessage("Fecha final requerida");

        RuleFor(m => m.Costo)
            .GreaterThanOrEqualTo(0)
            .When(m => m.Costo.HasValue)
            .WithMessage("Costo no puede ser negativo");

        RuleFor(m => m)
            .Must(m =>
                !m.FechaMantenimiento.HasValue
                || !m.FechaFinalMantenimiento.HasValue
                || m.FechaMantenimiento.Value <= m.FechaFinalMantenimiento.Value
            )
            .WithMessage("Fecha inicio no puede ser posterior a fecha final");
    }
}
