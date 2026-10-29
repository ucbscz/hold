using FluentValidation;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Application.Features.Gavetero;

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
