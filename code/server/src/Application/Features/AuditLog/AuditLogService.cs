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

    public async Task<Result<List<AuditLogDto>>> GetFiltered(
        string? entidad,
        string? carnet,
        DateTime? desde,
        DateTime? hasta
    )
    {
        var logs = await _repository.GetFiltered(entidad, carnet, desde, hasta);

        return Result<List<AuditLogDto>>.Success(logs);
    }

    private (string Carnet, string Nombre) GetActor() =>
        (
            _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value ?? "sistema",
            _httpContextAccessor.HttpContext?.User.FindFirst("nombre")?.Value ?? string.Empty
        );
}
