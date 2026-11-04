using System.Text.Json;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Inventario.Application;
using UCB_Hold.Server.Modules.Inventario.Domain;
using UCB_Hold.Server.Modules.Prestamos.Application.State;
using UCB_Hold.Server.Modules.Prestamos.Domain;
using UCB_Hold.Server.Modules.Prestamos.Infrastructure;

namespace UCB_Hold.Server.Modules.Prestamos.Application;

public sealed record RetornoEquipos(string? AuditDetail, string? UserMessage, bool HasDamagedEquipment);

public sealed class RetornoEquiposService
{
    private readonly PrestamoEstadoRepository _states;
    private readonly EquipmentLabels _labels;

    public RetornoEquiposService(PrestamoEstadoRepository states, EquipmentLabels labels)
    {
        _states = states;
        _labels = labels;
    }

    public async Task<RetornoEquipos> Apply(
        int id,
        string? observacion,
        PrestamoDto? body,
        EstadoPrestamo previousState,
        CancellationToken cancellationToken
    )
    {
        if (body?.EquiposRetorno == null || body.EquiposRetorno.Count == 0)
            return new RetornoEquipos(
                AuditChangeDetail.Build(
                    new { EstadoPrestamo = previousState },
                    new { EstadoPrestamo = EstadoPrestamo.Finalizado }
                ),
                null,
                false
            );

        var statesByCodigoImt = new Dictionary<int, EstadoEquipo>();

        foreach (var item in body.EquiposRetorno)
        {
            if (int.TryParse(item.CodigoImt, out var codigoImt))
                statesByCodigoImt[codigoImt] = EstadoEquipoNombre.Parse(item.EstadoEquipo);
        }

        var appliedReturns = await _states.ApplyEstadoEquipoRetorno(
            id,
            statesByCodigoImt,
            cancellationToken
        );
        var affectedEquipment = appliedReturns
            .Where(equipment => equipment.EstadoEquipo != EstadoEquipoNombre.Operativo)
            .ToList();

        var auditDetail = JsonSerializer.Serialize(
            new
            {
                observacion,
                cambios = new[]
                {
                    new
                    {
                        campo = "Estado del préstamo",
                        protegido = false,
                        anterior = PrestamoState.ToText(previousState),
                        nuevo = PrestamoState.ToText(EstadoPrestamo.Finalizado),
                    },
                },
                equipos = appliedReturns.Select(appliedReturn => new
                {
                    codigo = appliedReturn.CodigoImt,
                    nombre = appliedReturn.NombreGrupoEquipo,
                    estado = appliedReturn.EstadoEquipo,
                }),
            }
        );

        if (affectedEquipment.Count == 0)
            return new RetornoEquipos(auditDetail, null, false);

        var inoperableEquipment = affectedEquipment
            .Where(equipment => equipment.EstadoEquipo == EstadoEquipoNombre.Inoperativo)
            .ToList();
        var selectedEquipment = inoperableEquipment.Count > 0
            ? inoperableEquipment
            : affectedEquipment;

        var equipmentNames = string.Join(
            ", ",
            selectedEquipment.Select(equipment =>
                $"{equipment.NombreGrupoEquipo ?? "Equipo"} {_labels.InternalCode(equipment.CodigoImt)}"
            )
        );
        var stateText = inoperableEquipment.Count > 0
            ? EstadoEquipoNombre.Inoperativo
            : "con observación";
        var message = selectedEquipment.Count == 1
            ? $"Se ha marcado {equipmentNames} como {stateText} en tu préstamo."
            : $"Se han marcado estos equipos como {stateText} en tu préstamo: {equipmentNames}.";

        return new RetornoEquipos(auditDetail, message, true);
    }
}
