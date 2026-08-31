using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Core.Entities;

namespace IMT_Reservas.Server.Application.Features.Inventario;

public sealed class AmbienteMapper : IMapper<Ambiente, CatalogoInventarioDto>
{
    public CatalogoInventarioDto ToDto(Ambiente entity) => ProjectTo(new[] { entity }.AsQueryable()).Single();
    public Ambiente ToEntity(CatalogoInventarioDto dto) => new()
    {
        Id = dto.Id ?? 0,
        Nombre = dto.Nombre,
        CarnetAdministrador = string.IsNullOrWhiteSpace(dto.CarnetAdministrador) ? null : dto.CarnetAdministrador.Trim()
    };
    public IQueryable<CatalogoInventarioDto> ProjectTo(IQueryable<Ambiente> source) => source.Select(a => new CatalogoInventarioDto
    {
        Id = a.Id,
        Nombre = a.Nombre,
        CarnetAdministrador = a.CarnetAdministrador,
        NombreAdministrador = a.Administrador == null ? null : a.Administrador.Nombre + " " + a.Administrador.ApellidoPaterno + " " + a.Administrador.ApellidoMaterno
    });
}
