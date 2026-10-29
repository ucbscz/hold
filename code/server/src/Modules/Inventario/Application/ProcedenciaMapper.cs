using IMT_Reservas.Server.Application.Abstraction;
using IMT_Reservas.Server.Core.Entities;
using Riok.Mapperly.Abstractions;

namespace IMT_Reservas.Server.Application.Features.Inventario;

[Mapper]
public partial class ProcedenciaMapper : IMapper<Procedencia, CatalogoInventarioDto>
{
    public partial CatalogoInventarioDto ToDto(Procedencia entity);
    public partial Procedencia ToEntity(CatalogoInventarioDto dto);
    public partial IQueryable<CatalogoInventarioDto> ProjectTo(IQueryable<Procedencia> source);
}
