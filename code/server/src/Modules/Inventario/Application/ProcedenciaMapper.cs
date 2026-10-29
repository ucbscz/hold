using Riok.Mapperly.Abstractions;
using IMT_Reservas.Server.Modules.Inventario.Domain;
using IMT_Reservas.Server.Shared.Kernel;

namespace IMT_Reservas.Server.Modules.Inventario.Application;

[Mapper]
public partial class ProcedenciaMapper : IMapper<Procedencia, CatalogoInventarioDto>
{
    public partial CatalogoInventarioDto ToDto(Procedencia entity);
    public partial Procedencia ToEntity(CatalogoInventarioDto dto);
    public partial IQueryable<CatalogoInventarioDto> ProjectTo(IQueryable<Procedencia> source);
}
