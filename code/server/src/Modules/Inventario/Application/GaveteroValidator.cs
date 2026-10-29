using FluentValidation;
using Microsoft.EntityFrameworkCore;
using IMT_Reservas.Server.Shared.Persistence;

namespace IMT_Reservas.Server.Modules.Inventario.Application;

public class GaveteroValidator : AbstractValidator<GaveteroDto>
{
    public GaveteroValidator(ApplicationDbContext dbContext)
    {
        RuleFor(g => g.Nombre).NotEmpty().WithMessage("Nombre requerido");

        RuleFor(g => g.NombreMueble)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("NombreMueble requerido")
            .MustAsync(
                async (nombre, cancellationToken) =>
                    await dbContext.Muebles.AnyAsync(
                        m => m.Nombre == nombre && !m.EstadoEliminado,
                        cancellationToken
                    )
            )
            .WithMessage("Mueble no existe");
    }
}
