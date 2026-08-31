using IMT_Reservas.Server.Application.Features.GrupoEquipo;
using IMT_Reservas.Server.Application.Features.Componente;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using GrupoEquipoEntity = IMT_Reservas.Server.Core.Entities.GrupoEquipo;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class GrupoEquipoRepository : Repository<GrupoEquipoEntity, GrupoEquipoDto>
{
    private readonly EquipoRepository _equipos;

    public Task<List<ComponenteDto>> GetComponents(int id, int page, CancellationToken token) =>
        (from componente in DbContext.Componentes.AsNoTracking()
         join equipo in DbContext.Equipos.AsNoTracking() on componente.IdEquipo equals equipo.Id
         where equipo.IdGrupoEquipo == id
         orderby componente.Nombre, componente.Id
         select new ComponenteDto
         {
             Id = componente.Id,
             Nombre = componente.Nombre,
             Modelo = componente.Modelo,
             Tipo = componente.Tipo,
             Descripcion = componente.Descripcion,
             PrecioReferencia = componente.PrecioReferencia,
             IdEquipo = equipo.Id,
             CodigoImtEquipo = equipo.CodigoImt.ToString(),
             UrlDataSheet = componente.UrlDataSheet
         }).Skip((page - 1) * 100).Take(100).ToListAsync(token);

    public GrupoEquipoRepository(
        ApplicationDbContext dbContext,
        GrupoEquipoMapper mapper,
        EquipoRepository equipos
    )
        : base(dbContext, mapper) => _equipos = equipos;

    public async Task<List<GrupoEquipoDto>> Search(string? nombre = null, string? categoria = null)
    {
        var query = DbContext.GruposEquipos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(nombre))
            query = query.Where(g =>
                g.Nombre.Contains(nombre) || g.Modelo.Contains(nombre) || g.Marca.Contains(nombre)
            );

        if (!string.IsNullOrWhiteSpace(categoria))
            query = query.Where(g => g.Categoria != null && g.Categoria.Nombre == categoria);

        return await ProjectTo(query).ToListAsync();
    }

    public async Task<List<GrupoEquipoDto>> GetByIds(
        IReadOnlyList<int> ids,
        string? categoria = null
    )
    {
        if (ids.Count == 0)
            return [];

        var query = DbContext.GruposEquipos.AsNoTracking().Where(g => ids.Contains(g.Id));

        if (!string.IsNullOrWhiteSpace(categoria))
            query = query.Where(g => g.Categoria != null && g.Categoria.Nombre == categoria);

        var encontrados = await ProjectTo(query).ToDictionaryAsync(g => g.Id!.Value);

        return ids.Select(id => encontrados.GetValueOrDefault(id))
            .OfType<GrupoEquipoDto>()
            .ToList();
    }

    public async Task<int?> FindCategoriaIdByNombre(string nombre) =>
        await DbContext
            .Categorias.AsNoTracking()
            .Where(c => c.Nombre == nombre && !c.EstadoEliminado)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

    protected override async Task CascadeDelete(GrupoEquipoEntity grupo) =>
        await CascadeThrough(_equipos, (Equipo e) => e.IdGrupoEquipo == grupo.Id);
}
