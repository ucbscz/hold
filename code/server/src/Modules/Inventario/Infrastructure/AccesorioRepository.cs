using System.Globalization;
using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Accesorio;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using AccesorioEntity = IMT_Reservas.Server.Core.Entities.Accesorio;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class AccesorioRepository : Repository<AccesorioEntity, AccesorioDto>
{
    public AccesorioRepository(ApplicationDbContext dbContext, AccesorioMapper mapper)
        : base(dbContext, mapper) { }

    public override async Task<Result<AccesorioDto>> Create(AccesorioEntity entity)
    {
        var result = await base.Create(entity);

        if (!result.IsSuccess)
            return result;

        return await Get(entity.Id);
    }

    public override async Task<Result<AccesorioDto>> Update(AccesorioEntity entity)
    {
        var result = await base.Update(entity);

        if (!result.IsSuccess)
            return result;

        return await Get(entity.Id);
    }

    public override async Task<Result<List<AccesorioDto>>> GetAll()
    {
        var rows = await DbContext
            .Accesorios.AsNoTracking()
            .Join(
                DbContext.Equipos,
                accesorio => accesorio.IdEquipo,
                equipo => equipo.Id,
                (accesorio, equipo) =>
                    new
                    {
                        Accesorio = accesorio,
                        equipo.CodigoImt,
                        NombreEquipo = equipo.Descripcion,
                    }
            )
            .ToListAsync();

        var dtos = rows
            .Select(row => ToDto(row.Accesorio, row.CodigoImt, row.NombreEquipo))
            .ToList();

        return Result<List<AccesorioDto>>.Success(dtos);
    }

    public override async Task<Result<AccesorioDto>> Get(int id)
    {
        var row = await DbContext
            .Accesorios.AsNoTracking()
            .Where(accesorio => accesorio.Id == id)
            .Join(
                DbContext.Equipos,
                accesorio => accesorio.IdEquipo,
                equipo => equipo.Id,
                (accesorio, equipo) =>
                    new
                    {
                        Accesorio = accesorio,
                        equipo.CodigoImt,
                        NombreEquipo = equipo.Descripcion,
                    }
            )
            .FirstOrDefaultAsync();

        var dto = row == null ? null : ToDto(row.Accesorio, row.CodigoImt, row.NombreEquipo);

        return dto == null ? Result<AccesorioDto>.NotFound() : Result<AccesorioDto>.Success(dto);
    }

    private static AccesorioDto ToDto(
        AccesorioEntity accesorio,
        int codigoImt,
        string? nombreEquipo
    ) =>
        new()
        {
            Id = accesorio.Id,
            Nombre = accesorio.Nombre,
            Modelo = accesorio.Modelo,
            Tipo = accesorio.Tipo,
            Descripcion = accesorio.Descripcion,
            Precio = accesorio.Precio,
            UrlDataSheet = accesorio.UrlDataSheet,
            IdEquipo = accesorio.IdEquipo,
            CodigoImtEquipoAsociado = codigoImt.ToString(CultureInfo.InvariantCulture),
            NombreEquipoAsociado = nombreEquipo,
        };
}
