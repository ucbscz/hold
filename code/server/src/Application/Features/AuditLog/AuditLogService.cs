using Ardalis.Result;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Application.Features.AuditLog;

public class AuditLogService
{
    private readonly AuditLogRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(AuditLogRepository repository, IHttpContextAccessor httpContextAccessor)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task Log(
        AuditAccion accion,
        string entidad,
        string? entidadId,
        string? detalle = null,
        bool saveChanges = true
    )
    {
        var actor = GetActor();
        await _repository.WriteLog(
            accion,
            entidad,
            entidadId,
            detalle,
            actor.Carnet,
            actor.Nombre,
            saveChanges
        );
    }

    public async Task LogMany(IReadOnlyCollection<AuditEntry> entries)
    {
        if (entries.Count == 0)
            return;

        var actor = GetActor();
        await _repository.WriteMany(entries, actor.Carnet, actor.Nombre);
    }

    public async Task LogAsSystem(
        AuditAccion accion,
        string entidad,
        string? entidadId,
        string? detalle = null
    ) => await _repository.WriteLog(
        accion,
        entidad,
        entidadId,
        detalle,
        "sistema",
        "Sistema"
    );

    public async Task<Result<List<AuditLogDto>>> GetFiltered(
        string? entidad,
        string? actor,
        string? accion,
        DateTime? desde,
        DateTime? hasta,
        string? buscar = null
    )
    {
        var logs = await _repository.GetFiltered(entidad, actor, accion, desde, hasta, buscar);

        return Result<List<AuditLogDto>>.Success(logs);
    }

    private (string Carnet, string Nombre) GetActor()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var carnet = user?.FindFirst("sub")?.Value;

        return string.IsNullOrWhiteSpace(carnet)
            ? ("sistema", "Sistema")
            : (carnet, user?.FindFirst("nombre")?.Value ?? carnet);
    }
}
