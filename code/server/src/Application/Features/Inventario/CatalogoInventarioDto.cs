using FluentValidation;

namespace IMT_Reservas.Server.Application.Features.Inventario;

public class CatalogoInventarioDto
{
    public int? Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? CarnetAdministrador { get; set; }
    public string? NombreAdministrador { get; set; }
}

public sealed class CatalogoInventarioValidator : AbstractValidator<CatalogoInventarioDto>
{
    public CatalogoInventarioValidator() =>
        RuleFor(c => c.Nombre).NotEmpty().MaximumLength(255);
}
