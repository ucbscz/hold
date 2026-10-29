using FluentValidation;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Application.Features.Carrera;

public class CarreraValidator : AbstractValidator<CarreraDto>
{
    public CarreraValidator(ApplicationDbContext dbContext)
    {
        RuleFor(carrera => carrera.Nombre).NotEmpty().WithMessage("Nombre requerido");

        RuleFor(carrera => carrera)
            .MustAsync(
                async (dto, cancellationToken) =>
                {
                    var existing = await dbContext
                        .Carreras.AsNoTracking()
                        .FirstOrDefaultAsync(
                            c => c.Nombre == dto.Nombre && !c.EstadoEliminado,
                            cancellationToken
                        );
                    return existing == null || existing.Id == dto.Id;
                }
            )
            .When(carrera => !string.IsNullOrEmpty(carrera.Nombre))
            .WithMessage("Ya existe una carrera con ese nombre");
    }
}
