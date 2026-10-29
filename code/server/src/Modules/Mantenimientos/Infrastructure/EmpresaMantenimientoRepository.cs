using Microsoft.EntityFrameworkCore;
using EmpresaMantenimientoEntity = UCB_Hold.Server.Modules.Mantenimientos.Domain.EmpresaMantenimiento;
using UCB_Hold.Server.Modules.Mantenimientos.Application;
using UCB_Hold.Server.Modules.Mantenimientos.Domain;
using UCB_Hold.Server.Shared.Kernel;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Mantenimientos.Infrastructure;

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
