using System.Globalization;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Infrastructure.Jobs;

public sealed class EstadoMantenimientoJob
{
    private readonly MantenimientoRepository _repository;
    private readonly AuditLogService _audit;

    public EstadoMantenimientoJob(
        MantenimientoRepository repository,
        AuditLogService audit
    )
    {
        _repository = repository;
        _audit = audit;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        var changes = await _repository.SyncEquipmentStates(DateTime.UtcNow, cancellationToken);

        if (changes.Count == 0)
            return;

        await _audit.LogMany(
            changes
                .Select(change => new AuditEntry(
                    AuditAccion.Editar,
                    nameof(Equipo),
                    change.Id.ToString(CultureInfo.InvariantCulture),
                    change.Estado == EstadoEquipo.EnMantenimiento
                        ? $"Equipo IMT {change.CodigoImt} ingresó automáticamente a mantenimiento"
                        : $"Equipo IMT {change.CodigoImt} finalizó su mantenimiento y volvió a operativo"
                ))
                .ToList()
        );
    }
}
