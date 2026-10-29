using System.Data;
using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using PrestamoEntity = IMT_Reservas.Server.Modules.Prestamos.Domain.Prestamo;
using IMT_Reservas.Server.Modules.Prestamos.Application;
using IMT_Reservas.Server.Modules.Prestamos.Domain;
using IMT_Reservas.Server.Shared.Kernel;
using IMT_Reservas.Server.Shared.Persistence;
using IMT_Reservas.Server.Shared.Security;

namespace IMT_Reservas.Server.Modules.Prestamos.Infrastructure;

public class PrestamoRepository : Repository<PrestamoEntity, PrestamoDto>
{
    private readonly ContractHtmlProcessor _contractHtml;
    private readonly PrestamoReadRepository _queries;
    private readonly PrestamoDisponibilidadRepository _availability;
    private readonly SensitiveDataProtector _sensitiveData;

    public PrestamoRepository(
        ApplicationDbContext dbContext,
        PrestamoMapper mapper,
        ContractHtmlProcessor contractHtml,
        PrestamoReadRepository queries,
        PrestamoDisponibilidadRepository availability,
        SensitiveDataProtector sensitiveData
    )
        : base(dbContext, mapper)
    {
        _contractHtml = contractHtml;
        _queries = queries;
        _availability = availability;
        _sensitiveData = sensitiveData;
    }

    public override Task<Result<List<PrestamoDto>>> GetAll() => _queries.GetAll();

    public override Task<Result<PrestamoDto>> Get(int id) => _queries.Get(id);

    protected override Task CascadeDelete(PrestamoEntity loan) =>
        CascadeLeaf<DetallePrestamo>(detail => detail.IdPrestamo == loan.Id);

    public Task<PrestamoEntity?> FindById(
        int id,
        CancellationToken cancellationToken = default
    ) => DbContext.Prestamos.FirstOrDefaultAsync(
        loan => loan.Id == id,
        cancellationToken
    );

    public async Task<Result<PrestamoEntity>> CreateReservation(
        PrestamoEntity entity,
        IReadOnlyCollection<int> groupIds,
        string? contractHtml,
        CancellationToken cancellationToken = default
    )
    {
        await using var transaction = DbContext.Database.IsRelational()
            ? await DbContext.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken
            )
            : null;

        try
        {
            var durationViolation = await GetLoanDurationViolation(
                groupIds,
                entity.FechaPrestamoEsperada,
                entity.FechaDevolucionEsperada,
                cancellationToken
            );

            if (durationViolation != null)
            {
                if (transaction != null)
                    await transaction.RollbackAsync(cancellationToken);

                return Result<PrestamoEntity>.Error(durationViolation);
            }

            var requiredByGroup = groupIds
                .GroupBy(id => id)
                .ToDictionary(group => group.Key, group => group.Count());
            if (
                !await _availability.HasAvailableEquipos(
                    requiredByGroup,
                    entity.FechaPrestamoEsperada,
                    entity.FechaDevolucionEsperada,
                    cancellationToken
                )
            )
            {
                if (transaction != null)
                    await transaction.RollbackAsync(cancellationToken);

                return Result<PrestamoEntity>.Error(
                    "No hay suficientes unidades disponibles en el horario seleccionado"
                );
            }

            DbContext.Prestamos.Add(entity);
            await DbContext.SaveChangesAsync(cancellationToken);

            DbContext.DetallesPrestamos.AddRange(
                groupIds.Select(groupId => new DetallePrestamo
                {
                    IdPrestamo = entity.Id,
                    IdGrupoEquipo = groupId,
                    EstadoEliminado = false,
                })
            );
            await DbContext.SaveChangesAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(contractHtml))
            {
                var equipment = await _queries.GetContractEquipment(
                    entity.Id,
                    cancellationToken
                );
                var contract = new Contrato
                {
                    ContratoHtml = _sensitiveData.Protect(
                        _contractHtml.RenderEquipment(contractHtml, equipment)
                    ),
                };

                await SaveContractAsSystem(contract, cancellationToken);
                entity.IdContrato = contract.Id;
                await DbContext.SaveChangesAsync(cancellationToken);
            }

            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);

            return Result<PrestamoEntity>.Success(entity);
        }
        catch (ArgumentException exception)
        {
            await RollbackCreate(transaction, entity.Id, cancellationToken);
            return Result<PrestamoEntity>.Error(exception.Message);
        }
        catch (DbUpdateException)
        {
            await RollbackCreate(transaction, entity.Id, cancellationToken);
            return Result<PrestamoEntity>.Error(
                "La disponibilidad cambió mientras se procesaba la reserva. Intente nuevamente."
            );
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            await RollbackCreate(transaction, entity.Id, cancellationToken);
            return Result<PrestamoEntity>.Error(
                "Otra reserva modificó la disponibilidad. Intente nuevamente."
            );
        }
    }

    public async Task UpdateTracked(
        PrestamoEntity entity,
        CancellationToken cancellationToken = default
    )
    {
        DbContext.Prestamos.Update(entity);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string?> GetLoanDurationViolation(
        IReadOnlyCollection<int> groupIds,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken
    )
    {
        var requestedIds = groupIds.Distinct().ToArray();
        var groups = await DbContext
            .GruposEquipos.AsNoTracking()
            .Where(group => requestedIds.Contains(group.Id))
            .Select(group => new
            {
                group.Nombre,
                group.TiempoMaximoPrestamoDias,
            })
            .ToListAsync(cancellationToken);

        if (groups.Count != requestedIds.Length)
            return "Uno o más grupos de equipos no existen o no están disponibles";

        return LimiteDuracionPrestamo.Validar(
            groups.Select(group => (group.Nombre, group.TiempoMaximoPrestamoDias)),
            endDate - startDate
        );
    }

    private async Task SaveContractAsSystem(
        Contrato contract,
        CancellationToken cancellationToken
    )
    {
        await using var _ = await DbContext.RunAsSystemAsync(cancellationToken);
        DbContext.Contratos.Add(contract);
        await DbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RollbackCreate(
        IDbContextTransaction? transaction,
        int prestamoId,
        CancellationToken cancellationToken
    )
    {
        if (transaction != null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        var details = await DbContext
            .DetallesPrestamos.Where(detail => detail.IdPrestamo == prestamoId)
            .ToListAsync(cancellationToken);
        DbContext.DetallesPrestamos.RemoveRange(details);

        var loan = await DbContext.Prestamos.FirstOrDefaultAsync(
            item => item.Id == prestamoId,
            cancellationToken
        );

        if (loan != null)
            DbContext.Prestamos.Remove(loan);

        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
