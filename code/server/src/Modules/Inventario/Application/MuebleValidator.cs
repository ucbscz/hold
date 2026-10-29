using FluentValidation;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Application.Features.Mueble;

public class MuebleValidator : AbstractValidator<MuebleDto>
{
    public MuebleValidator(ApplicationDbContext db)
    {
        RuleFor(m => m.IdAmbiente).MustAsync(async (id, ct) => !id.HasValue || await db.Ambientes.AnyAsync(a => a.Id == id.Value, ct))
            .WithMessage("Selecciona un ambiente existente");
        RuleFor(m => m.Nombre).NotEmpty().WithMessage("Nombre requerido");

        RuleFor(m => m.Costo)
            .GreaterThanOrEqualTo(0)
            .When(m => m.Costo.HasValue)
            .WithMessage("Costo no puede ser negativo");
    }
}
