using Riok.Mapperly.Abstractions;
using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Inventario.Application;

[Mapper]
public partial class ProcedenciaMapper : IMapper<Procedencia, CatalogoInventarioDto>
{
    public partial CatalogoInventarioDto ToDto(Procedencia entity);
    public partial Procedencia ToEntity(CatalogoInventarioDto dto);
    public partial IQueryable<CatalogoInventarioDto> ProjectTo(IQueryable<Procedencia> source);
}
