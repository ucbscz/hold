using FluentValidation;
using UCB_Hold.Server.Modules.Mantenimientos.Infrastructure;

namespace UCB_Hold.Server.Modules.Mantenimientos.Application;

public class EmpresaMantenimientoValidator : AbstractValidator<EmpresaMantenimientoDto>
{
    public EmpresaMantenimientoValidator(EmpresaMantenimientoRepository repository)
    {
        RuleFor(empresa => empresa.NombreEmpresa).NotEmpty().WithMessage("NombreEmpresa requerido");

        RuleFor(empresa => empresa.Nit)
            .MustAsync(async (dto, nit, _) => !await repository.ExistsByNit(nit!, dto.Id))
            .When(empresa => !string.IsNullOrWhiteSpace(empresa.Nit))
            .WithMessage("NIT ya registrado");
    }
}
