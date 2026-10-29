using FluentValidation;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Application.Features.EmpresaMantenimiento;

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
