using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Contrato;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using ContratoEntity = IMT_Reservas.Server.Core.Entities.Contrato;
using PrestamoEntity = IMT_Reservas.Server.Core.Entities.Prestamo;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class ContratoRepository : Repository<ContratoEntity, ContratoDto>
{
    public ContratoRepository(ApplicationDbContext dbContext, ContratoMapper mapper)
        : base(dbContext, mapper) { }

    public async Task<Result<ContratoEntity>> GetEntityByPrestamoId(int prestamoId)
    {
        var loan = await DbContext
            .Prestamos.AsNoTracking()
            .FirstOrDefaultAsync(loan => loan.Id == prestamoId);

        if (loan == null || !loan.IdContrato.HasValue)
            return Result<ContratoEntity>.Error("Préstamo no encontrado o no tiene contrato");

        var contract = await DbContext
            .Contratos.AsNoTracking()
            .FirstOrDefaultAsync(contract => contract.Id == loan.IdContrato.Value);

        if (contract == null)
            return Result<ContratoEntity>.Error("Contrato no encontrado");

        return Result<ContratoEntity>.Success(contract);
    }

    public async Task<bool> CanAccess(int prestamoId, string carnet, bool isAdmin) =>
        isAdmin
        || await DbContext.Prestamos.AsNoTracking().AnyAsync(loan =>
            loan.Id == prestamoId && loan.Carnet == carnet
        );

    public override async Task<Result<object>> Delete(int id)
    {
        var loan = await DbContext.Prestamos.FirstOrDefaultAsync(loan => loan.Id == id);

        if (loan == null || !loan.IdContrato.HasValue)
            return Result<object>.Error("Préstamo no encontrado o no tiene contrato");

        var contract = await DbContext.Contratos.FirstOrDefaultAsync(contract =>
            contract.Id == loan.IdContrato.Value
        );

        if (contract == null)
            return Result<object>.Error("Contrato no encontrado");

        DbContext.Contratos.Remove(contract);
        loan.IdContrato = null;
        DbContext.Prestamos.Update(loan);
        await DbContext.SaveChangesAsync();

        return Result<object>.Success(new { });
    }

    public async Task<PrestamoEntity?> FindPrestamoById(int prestamoId) =>
        await DbContext.Prestamos.FirstOrDefaultAsync(loan => loan.Id == prestamoId);

    public async Task SavePrestamo(PrestamoEntity loan)
    {
        DbContext.Prestamos.Update(loan);
        await DbContext.SaveChangesAsync();
    }
}
