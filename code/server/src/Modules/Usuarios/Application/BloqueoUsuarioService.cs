using Ardalis.Result;
using UsuarioEntity = UCB_Hold.Server.Modules.Usuarios.Domain.Usuario;
using UCB_Hold.Server.Modules.Auditoria.Application;
using UCB_Hold.Server.Modules.Notificaciones.Application;
using UCB_Hold.Server.Modules.Usuarios.Infrastructure;
using UCB_Hold.Server.Shared.Caching;

namespace UCB_Hold.Server.Modules.Usuarios.Application;

public sealed class BloqueoUsuarioService
{
    private readonly UsuarioRepository _repository;
    private readonly UsuarioReadRepository _queries;
    private readonly AuditLogService _audit;
    private readonly NotificacionService _notifications;
    private readonly CacheRepository _cacheRepository;

    public BloqueoUsuarioService(
        UsuarioRepository repository,
        UsuarioReadRepository queries,
        AuditLogService audit,
        NotificacionService notifications,
        CacheRepository cacheRepository
    )
    {
        _repository = repository;
        _queries = queries;
        _audit = audit;
        _notifications = notifications;
        _cacheRepository = cacheRepository;
    }

    public async Task<Result<object>> SetBlocked(
        string carnet,
        bool isBlocked,
        string? blockReason,
        bool isAdmin,
        string? actorCarnet = null,
        bool isLabAdmin = false,
        CancellationToken cancellationToken = default
    )
    {
        if (!isAdmin)
            return Result<object>.Forbidden();

        var user = await _repository.GetTrackedByCarnet(carnet, cancellationToken);

        if (user == null)
            return Result<object>.NotFound();

        if (isLabAdmin && ManagementRoles.Includes(user.Rol))
            return Result<object>.Forbidden();

        var previousAuditState = new { user.Bloqueado, user.MotivoBloqueo };
        user.Bloqueado = isBlocked;
        user.MotivoBloqueo = isBlocked ? blockReason : null;
        await _repository.UpdateEntity(
            user,
            saveChanges: false,
            cancellationToken: cancellationToken
        );

        await _audit.Log(
            isBlocked ? AuditAccion.Bloquear : AuditAccion.Desbloquear,
            typeof(UsuarioEntity).Name,
            carnet,
            AuditChangeDetail.Build(
                previousAuditState,
                new { user.Bloqueado, user.MotivoBloqueo }
            ),
            saveChanges: false
        );

        await Notify(carnet, isBlocked, blockReason, actorCarnet, cancellationToken);

        await _repository.SaveChanges(cancellationToken);
        _ = _cacheRepository.Remove(CacheKeys.Usuario(carnet), CancellationToken.None);

        return Result<object>.Success(null!);
    }

    private async Task Notify(
        string carnet,
        bool isBlocked,
        string? blockReason,
        string? actorCarnet,
        CancellationToken cancellationToken
    )
    {
        var actorName = string.IsNullOrWhiteSpace(actorCarnet)
            ? null
            : await _queries.GetDisplayName(actorCarnet, cancellationToken);
        var notificationType = isBlocked
            ? TipoNotificacion.UsuarioBloqueado
            : TipoNotificacion.UsuarioDesbloqueado;
        var notificationTitle = isBlocked
            ? "Cuenta bloqueada para reservas"
            : "Cuenta desbloqueada para reservas";
        var notificationReason = isBlocked
            ? blockReason ?? "Bloqueo administrativo para nuevas reservas."
            : "Desbloqueo administrativo de la cuenta.";
        var notificationContent = isBlocked
            ? string.IsNullOrWhiteSpace(blockReason)
                ? "Tu cuenta fue bloqueada para nuevas reservas. Contacta con un administrador para revisar tu caso."
                : $"{blockReason}. Contacta con un administrador para revisar tu caso."
            : "Tu cuenta fue desbloqueada. Ya puedes realizar nuevas reservas.";

        await _notifications.Create(
            carnet,
            notificationType,
            notificationTitle,
            notificationContent,
            NotificacionService.BuildEmitterDetail(actorName ?? NotificacionService.SystemEmitter, notificationReason),
            saveChanges: false
        );
    }
}
