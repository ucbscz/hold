using Ardalis.Result;
using System.IdentityModel.Tokens.Jwt;
using UCB_Hold.Server.Modules.Auditoria.Infrastructure;
using UCB_Hold.Server.Shared.Kernel;

namespace UCB_Hold.Server.Modules.Auditoria.Application;

public class AuditLogService
{
    private static readonly string[] EntidadesVisiblesParaGestion = ["Prestamo", "Usuario"];

    private readonly AuditLogRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuditActorDirectory _users;

    public AuditLogService(
        AuditLogRepository repository,
        IHttpContextAccessor httpContextAccessor,
        IAuditActorDirectory users
    )
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
        _users = users;
    }

    public async Task Log(
        AuditAccion accion,
        string entidad,
        string? entidadId,
        string? detalle = null,
        bool saveChanges = true
    )
    {
        var actor = await GetActor();
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

        var actor = await GetActor();
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
        SystemActor.Carnet,
        SystemActor.Nombre
    );

    public async Task<Result<List<AuditLogDto>>> GetFiltered(
        bool esAdministrador,
        string? entidad,
        string? actor,
        string? accion,
        DateTime? desde,
        DateTime? hasta,
        string? buscar = null
    )
    {
        if (!esAdministrador && !EntidadesVisiblesParaGestion.Contains(entidad))
            return Result<List<AuditLogDto>>.Forbidden();

        var logs = await _repository.GetFiltered(entidad, actor, accion, desde, hasta, buscar);

        return Result<List<AuditLogDto>>.Success(logs);
    }

    private async Task<(string Carnet, string Nombre)> GetActor()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var carnet = user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrWhiteSpace(carnet))
            return (SystemActor.Carnet, SystemActor.Nombre);

        var name = await _users.GetDisplayName(carnet);
        return (carnet, string.IsNullOrWhiteSpace(name) ? carnet : name);
    }
}
