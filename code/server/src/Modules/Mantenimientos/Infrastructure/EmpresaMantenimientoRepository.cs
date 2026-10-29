using IMT_Reservas.Server.Application.Features.EmpresaMantenimiento;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using EmpresaMantenimientoEntity = IMT_Reservas.Server.Core.Entities.EmpresaMantenimiento;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class EmpresaMantenimientoRepository
    : Repository<EmpresaMantenimientoEntity, EmpresaMantenimientoDto>
{
    private readonly MantenimientoRepository _mantenimientos;

    public EmpresaMantenimientoRepository(
        ApplicationDbContext dbContext,
        EmpresaMantenimientoMapper mapper,
        MantenimientoRepository mantenimientos
    )
        : base(dbContext, mapper) => _mantenimientos = mantenimientos;

    public async Task<int?> FindIdByNombre(string nombre) =>
        await DbContext
            .EmpresasMantenimiento.AsNoTracking()
            .Where(e => e.Nombre == nombre && !e.EstadoEliminado)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync();

    public async Task<bool> ExistsByNit(string nit, int? excludeId = null) =>
        await DbContext.EmpresasMantenimiento.AnyAsync(e =>
            e.Nit == nit && !e.EstadoEliminado && e.Id != excludeId
        );

    protected override async Task CascadeDelete(EmpresaMantenimientoEntity empresa) =>
        await CascadeThrough(_mantenimientos, (Mantenimiento m) => m.IdEmpresa == empresa.Id);
}
