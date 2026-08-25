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
        var rows = await (
            from componente in DbContext.Componentes.AsNoTracking()
            join equipo in DbContext.Equipos.AsNoTracking()
                on componente.IdEquipo equals equipo.Id
                into equipoJoin
            from equipo in equipoJoin.DefaultIfEmpty()
            select new
            {
                Componente = componente,
                CodigoImtEquipo = equipo != null ? (int?)equipo.CodigoImt : null,
                NombreEquipo = equipo != null ? equipo.Descripcion : null,
            }
        ).ToListAsync();

        var dtos = rows
            .Select(row => ToDto(row.Componente, row.CodigoImtEquipo, row.NombreEquipo))
            .ToList();

        return Result<List<ComponenteDto>>.Success(dtos);
    }

    public override async Task<Result<ComponenteDto>> Get(int id)
    {
        var row = await (
            from componente in DbContext.Componentes.AsNoTracking().Where(c => c.Id == id)
            join equipo in DbContext.Equipos.AsNoTracking()
                on componente.IdEquipo equals equipo.Id
                into equipoJoin
            from equipo in equipoJoin.DefaultIfEmpty()
            select new
            {
                Componente = componente,
                CodigoImtEquipo = equipo != null ? (int?)equipo.CodigoImt : null,
                NombreEquipo = equipo != null ? equipo.Descripcion : null,
            }
        ).FirstOrDefaultAsync();

        var dto = row == null ? null : ToDto(row.Componente, row.CodigoImtEquipo, row.NombreEquipo);

        return dto == null ? Result<ComponenteDto>.NotFound() : Result<ComponenteDto>.Success(dto);
    }

    private static ComponenteDto ToDto(
        ComponenteEntity componente,
        int? codigoImtEquipo,
        string? nombreEquipo
    ) =>
        new()
        {
            Id = componente.Id,
            Nombre = componente.Nombre,
            Modelo = componente.Modelo,
            Tipo = componente.Tipo,
            Descripcion = componente.Descripcion,
            PrecioReferencia = componente.PrecioReferencia,
            IdEquipo = componente.IdEquipo,
            NombreEquipo = nombreEquipo,
            CodigoImtEquipo = codigoImtEquipo?.ToString(CultureInfo.InvariantCulture),
            UrlDataSheet = componente.UrlDataSheet,
        };
}
