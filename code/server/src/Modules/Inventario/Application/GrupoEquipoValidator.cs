using FluentValidation;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Application.Features.GrupoEquipo;

public class GrupoEquipoValidator : AbstractValidator<GrupoEquipoDto>
{
    public GrupoEquipoValidator(ApplicationDbContext dbContext)
    {
        RuleFor(g => g.Nombre).NotEmpty().WithMessage("Nombre requerido");
        RuleFor(g => g.Modelo).NotEmpty().WithMessage("Modelo requerido");
        RuleFor(g => g.Marca).NotEmpty().WithMessage("Marca requerida");

        RuleFor(g => g.TiempoMaximoPrestamoDias)
            .NotNull()
            .InclusiveBetween(1, 365)
            .WithMessage("El tiempo máximo de préstamo debe estar entre 1 y 365 días");

        RuleFor(g => g.IdCategoria)
            .Cascade(CascadeMode.Stop)
            .NotNull()
            .GreaterThan(0)
            .WithMessage("IdCategoria requerido")
            .MustAsync(
                async (id, cancellationToken) =>
                    await dbContext.Categorias.AnyAsync(
                        c => c.Id == id && !c.EstadoEliminado,
                        cancellationToken
                    )
            )
            .WithMessage("Categoría no existe");

        RuleFor(g => g)
            .MustAsync(
                async (dto, cancellationToken) =>
                {
                    var existing = await dbContext
                        .GruposEquipos.AsNoTracking()
                        .FirstOrDefaultAsync(
                            g =>
                                g.Nombre == dto.Nombre
                                && g.Modelo == dto.Modelo
                                && g.Marca == dto.Marca
                                && !g.EstadoEliminado,
                            cancellationToken
                        );
                    return existing == null || existing.Id == dto.Id;
                }
            )
            .When(g =>
                !string.IsNullOrEmpty(g.Nombre)
                && !string.IsNullOrEmpty(g.Modelo)
                && !string.IsNullOrEmpty(g.Marca)
            )
            .WithMessage("Ya existe un grupo con ese nombre, modelo y marca");
    }
}
