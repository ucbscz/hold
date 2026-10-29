using FluentValidation;

namespace UCB_Hold.Server.Modules.Prestamos.Application;

public class ContratoValidator : AbstractValidator<ContratoDto>
{
    public ContratoValidator() =>
        RuleFor(contrato => contrato.ContratoHtml)
            .NotEmpty()
            .WithMessage("Contenido de contrato requerido")
            .MaximumLength(ContractHtmlProcessor.MaxHtmlLength)
            .WithMessage("El contrato supera el tamaño máximo permitido");
}
