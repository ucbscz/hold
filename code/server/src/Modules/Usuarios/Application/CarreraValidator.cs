using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Usuarios.Application;

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

        RuleFor(carrera => carrera.Codigo)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Código requerido")
            .Matches("^[A-Z0-9]{2,10}$")
            .WithMessage("El código debe tener de 2 a 10 letras mayúsculas o números");

        RuleFor(carrera => carrera)
            .MustAsync(
                (dto, cancellationToken) =>
                    dbContext
                        .Carreras.AsNoTracking()
                        .AllAsync(c => c.Codigo != dto.Codigo || c.Id == dto.Id, cancellationToken)
            )
            .When(carrera => !string.IsNullOrEmpty(carrera.Codigo))
            .WithMessage("Ya existe una carrera con ese código");
    }
}
