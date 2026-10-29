using FluentValidation;

namespace IMT_Reservas.Server.Application.Features.Contrato;

public class ContratoValidator : AbstractValidator<ContratoDto>
{
    public ContratoValidator() =>
        RuleFor(contrato => contrato.ContratoHtml)
            .NotEmpty()
            .WithMessage("Contenido de contrato requerido")
            .MaximumLength(ContractHtmlProcessor.MaxHtmlLength)
            .WithMessage("El contrato supera el tamaño máximo permitido");
}
