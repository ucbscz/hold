using FluentValidation;
using IMT_Reservas.Server.Modules.Mantenimientos.Infrastructure;

namespace IMT_Reservas.Server.Modules.Mantenimientos.Application;

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
