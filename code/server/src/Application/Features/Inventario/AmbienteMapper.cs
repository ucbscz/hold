using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Core.Entities;
using Riok.Mapperly.Abstractions;

namespace IMT_Reservas.Server.Application.Features.Inventario;

[Mapper]
public partial class AmbienteMapper : IMapper<Ambiente, CatalogoInventarioDto>
{
    public partial CatalogoInventarioDto ToDto(Ambiente entity);
    public partial Ambiente ToEntity(CatalogoInventarioDto dto);
    public partial IQueryable<CatalogoInventarioDto> ProjectTo(IQueryable<Ambiente> source);
}
