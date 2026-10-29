using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Inventario.Application;

public class CategoriaValidator : AbstractValidator<CategoriaDto>
{
    public CategoriaValidator(ApplicationDbContext dbContext)
    {
        RuleFor(categoria => categoria.Nombre).NotEmpty().WithMessage("Nombre requerido");

        RuleFor(categoria => categoria)
            .MustAsync(
                async (dto, cancellationToken) =>
                {
                    var existing = await dbContext
                        .Categorias.AsNoTracking()
                        .FirstOrDefaultAsync(
                            c => c.Nombre == dto.Nombre && !c.EstadoEliminado,
                            cancellationToken
                        );
                    return existing == null || existing.Id == dto.Id;
                }
            )
            .When(categoria => !string.IsNullOrEmpty(categoria.Nombre))
            .WithMessage("Ya existe una categoría con ese nombre");
    }
}
