using System.Globalization;
using IMT_Reservas.Server.Modules.Auditoria.Application;
using IMT_Reservas.Server.Modules.Inventario.Application;
using IMT_Reservas.Server.Modules.Inventario.Domain;
using IMT_Reservas.Server.Modules.Mantenimientos.Infrastructure;

namespace IMT_Reservas.Server.Modules.Mantenimientos.Application;

public sealed class EstadoMantenimientoService
{
    private readonly MantenimientoRepository _repository;
    private readonly AuditLogService _audit;
    private readonly EquipmentLabels _labels;

    public EstadoMantenimientoService(
        MantenimientoRepository repository,
        AuditLogService audit,
        EquipmentLabels labels
    )
    {
        _repository = repository;
        _audit = audit;
        _labels = labels;
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
                        ? $"Equipo {_labels.InternalCode(change.CodigoImt)} ingresó automáticamente a mantenimiento"
                        : $"Equipo {_labels.InternalCode(change.CodigoImt)} finalizó su mantenimiento y volvió a operativo"
                ))
                .ToList()
        );
    }
}
