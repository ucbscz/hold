using System.Diagnostics.CodeAnalysis;
using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using GrupoEquipoEntity = UCB_Hold.Server.Modules.Inventario.Domain.GrupoEquipo;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Shared.Kernel;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Inventario.Infrastructure;

public class GrupoEquipoRepository : Repository<GrupoEquipoEntity, GrupoEquipoDto>
{
    private readonly EquipoRepository _equipos;

    [SuppressMessage("Globalization", "CA1305", Justification = SqlTranslation.Justification)]
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

        return await ConCodigosCarrera(await ProjectTo(query).ToListAsync());
    }

    public override async Task<Result<List<GrupoEquipoDto>>> GetAll()
    {
        var result = await base.GetAll();
        return result.IsSuccess ? Result<List<GrupoEquipoDto>>.Success(await ConCodigosCarrera(result.Value)) : result;
    }

    public override async Task<Result<GrupoEquipoDto>> Get(int id)
    {
        var result = await base.Get(id);
        if (result.IsSuccess)
            await ConCodigosCarrera([result.Value]);
        return result;
    }

    private async Task<List<GrupoEquipoDto>> ConCodigosCarrera(List<GrupoEquipoDto> grupos)
    {
        var ids = grupos.Select(g => g.Id).OfType<int>().ToList();
        if (ids.Count == 0)
            return grupos;

        var codigos = await (from equipo in DbContext.Equipos.AsNoTracking()
                             join carrera in DbContext.Carreras.AsNoTracking() on equipo.IdCarrera equals carrera.Id
                             where ids.Contains(equipo.IdGrupoEquipo)
                             select new { equipo.IdGrupoEquipo, carrera.Codigo })
            .Distinct()
            .ToListAsync();

        var porGrupo = codigos.ToLookup(c => c.IdGrupoEquipo, c => c.Codigo);
        foreach (var grupo in grupos)
            grupo.CodigosCarrera = porGrupo[grupo.Id ?? 0].Order(StringComparer.Ordinal).ToList();
        return grupos;
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

        var encontrados = (await ConCodigosCarrera(await ProjectTo(query).ToListAsync()))
            .ToDictionary(g => g.Id!.Value);

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
