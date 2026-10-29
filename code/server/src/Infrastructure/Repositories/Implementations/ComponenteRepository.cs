using System.Globalization;
using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Componente;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using ComponenteEntity = IMT_Reservas.Server.Core.Entities.Componente;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class ComponenteRepository : Repository<ComponenteEntity, ComponenteDto>
{
    public ComponenteRepository(ApplicationDbContext dbContext, ComponenteMapper mapper)
        : base(dbContext, mapper) { }

    public override async Task<Result<ComponenteDto>> Create(ComponenteEntity entity)
    {
        var result = await base.Create(entity);

        if (!result.IsSuccess)
            return result;

        return await Get(entity.Id);
    }

    public override async Task<Result<ComponenteDto>> Update(ComponenteEntity entity)
    {
        var result = await base.Update(entity);

        if (!result.IsSuccess)
            return result;

        return await Get(entity.Id);
    }

    public override async Task<Result<List<ComponenteDto>>> GetAll()
    {
        var rows = await QueryRows(DbContext.Componentes.AsNoTracking()).ToListAsync();

        return Result<List<ComponenteDto>>.Success(rows.ConvertAll(ToDto));
    }

    public override async Task<Result<ComponenteDto>> Get(int id)
    {
        var row = await QueryRows(DbContext.Componentes.AsNoTracking().Where(c => c.Id == id))
            .FirstOrDefaultAsync();

        return row == null ? Result<ComponenteDto>.NotFound() : Result<ComponenteDto>.Success(ToDto(row));
    }

    private IQueryable<ComponenteRow> QueryRows(IQueryable<ComponenteEntity> componentes) =>
        from componente in componentes
        join equipo in DbContext.Equipos.AsNoTracking()
            on componente.IdEquipo equals equipo.Id
            into equipoJoin
        from equipo in equipoJoin.DefaultIfEmpty()
        select new ComponenteRow(
            componente,
            equipo != null ? (int?)equipo.CodigoImt : null,
            equipo != null ? equipo.Descripcion : null
        );

    private static ComponenteDto ToDto(ComponenteRow row) =>
        new()
        {
            Id = row.Componente.Id,
            Nombre = row.Componente.Nombre,
            Modelo = row.Componente.Modelo,
            Tipo = row.Componente.Tipo,
            Descripcion = row.Componente.Descripcion,
            PrecioReferencia = row.Componente.PrecioReferencia,
            IdEquipo = row.Componente.IdEquipo,
            NombreEquipo = row.NombreEquipo,
            CodigoImtEquipo = row.CodigoImtEquipo?.ToString(CultureInfo.InvariantCulture),
            UrlDataSheet = row.Componente.UrlDataSheet,
        };

    private sealed record ComponenteRow(
        ComponenteEntity Componente,
        int? CodigoImtEquipo,
        string? NombreEquipo
    );
}
