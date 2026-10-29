using FluentValidation;

namespace IMT_Reservas.Server.Application.Features.Accesorio;

public class AccesorioValidator : AbstractValidator<AccesorioDto>
{
    public AccesorioValidator()
    {
        RuleFor(accesorio => accesorio.Nombre).NotEmpty().WithMessage("Nombre requerido");
        RuleFor(accesorio => accesorio.IdEquipo)
            .NotNull()
            .GreaterThan(0)
            .WithMessage("IdEquipo requerido");
    }
}
