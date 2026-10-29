using FluentValidation;

namespace IMT_Reservas.Server.Application.Features.Componente;

public class ComponenteValidator : AbstractValidator<ComponenteDto>
{
    public ComponenteValidator()
    {
        RuleFor(componente => componente.Nombre).NotEmpty().WithMessage("Nombre requerido");
        RuleFor(componente => componente.IdEquipo)
            .NotNull()
            .GreaterThan(0)
            .WithMessage("IdEquipo requerido");
    }
}
