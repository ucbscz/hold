using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.AuditLog;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Application.Features.GrupoEquipo;

public class ComentarioEquipoService
{
    private const int MaxContenidoLength = 1024;
    private const string OrdenAntiguos = "antiguos";
    private const string OrdenLikes = "likes";
    private readonly ComentarioEquipoRepository _repository;
    private readonly AuditLogService _audit;

    public ComentarioEquipoService(
        ComentarioEquipoRepository repository,
        AuditLogService audit
    )
    {
        _repository = repository;
        _audit = audit;
    }

    public async Task<Result<List<ComentarioEquipoDto>>> GetByGrupo(
        int grupoId,
        string? orden,
        string currentCarnet,
        bool isAdmin
    )
    {
        if (!await _repository.GrupoExiste(grupoId))
            return Result<List<ComentarioEquipoDto>>.NotFound();

        var comentarios = await _repository.GetByGrupo(grupoId, currentCarnet, isAdmin);

        return Result<List<ComentarioEquipoDto>>.Success(BuildThreads(comentarios, orden));
    }

    public async Task<Result<ComentarioEquipoDto>> Create(
        int grupoId,
        string carnet,
        CrearComentarioEquipoDto? dto
    )
    {
        var contenido = dto?.Contenido?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(carnet))
            return Result<ComentarioEquipoDto>.Error("Usuario requerido");
        if (string.IsNullOrWhiteSpace(contenido))
            return Result<ComentarioEquipoDto>.Error("Comentario requerido");
        if (contenido.Length > MaxContenidoLength)
            return Result<ComentarioEquipoDto>.Error("Comentario máximo 1024 caracteres");
        if (!await _repository.GrupoExiste(grupoId))
            return Result<ComentarioEquipoDto>.NotFound();
        if (
            dto?.IdComentarioPadre is int idComentarioPadre
            && !await _repository.ComentarioExiste(idComentarioPadre, grupoId)
        )
            return Result<ComentarioEquipoDto>.NotFound();

        return Result<ComentarioEquipoDto>.Created(
            await _repository.Add(grupoId, carnet, contenido, dto?.IdComentarioPadre)
        );
    }

    public async Task<Result<ComentarioEquipoDto>> ToggleLike(
        int grupoId,
        int comentarioId,
        string carnet,
        bool isAdmin
    )
    {
        if (string.IsNullOrWhiteSpace(carnet))
            return Result<ComentarioEquipoDto>.Unauthorized("Usuario requerido");
        if (!await _repository.ComentarioExiste(comentarioId, grupoId))
            return Result<ComentarioEquipoDto>.NotFound();

        var updated = await _repository.ToggleLike(comentarioId, grupoId, carnet, isAdmin);

        return updated is null
            ? Result<ComentarioEquipoDto>.NotFound()
            : Result<ComentarioEquipoDto>.Success(updated);
    }

    public async Task<Result<object>> Delete(
        int grupoId,
        int comentarioId,
        string carnet,
        bool isAdmin
    )
    {
        if (string.IsNullOrWhiteSpace(carnet))
            return Result<object>.Unauthorized("Usuario requerido");

        var comentario = await _repository.GetComentario(comentarioId, grupoId);

        if (comentario is null)
            return Result<object>.NotFound();
        if (
            !isAdmin
            && !string.Equals(comentario.CarnetUsuario, carnet, StringComparison.OrdinalIgnoreCase)
        )
            return Result<object>.Forbidden();

        await _repository.DeleteTree(comentarioId, grupoId);
        await _audit.Log(
            AuditAccion.EliminarComentario,
            "GrupoEquipo",
            grupoId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            $"Comentario {comentarioId} eliminado. Autor: {comentario.CarnetUsuario}"
        );

        return Result<object>.Success(null!);
    }

    private static List<ComentarioEquipoDto> BuildThreads(
        List<ComentarioEquipoDto> comentarios,
        string? orden
    )
    {
        var respuestasPorComentario = comentarios
            .Where(comentario => comentario.IdComentarioPadre.HasValue)
            .GroupBy(comentario => comentario.IdComentarioPadre!.Value)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.OrderBy(comentario => comentario.FechaCreacion).ToList()
            );
        var principales = comentarios
            .Where(comentario => !comentario.IdComentarioPadre.HasValue)
            .ToList();

        foreach (var comentario in principales)
            comentario.Respuestas = respuestasPorComentario.TryGetValue(comentario.Id, out var respuestas)
                ? respuestas
                : [];

        return NormalizeOrder(orden) switch
        {
            OrdenLikes => principales
                .OrderByDescending(comentario => comentario.Likes)
                .ThenByDescending(comentario => comentario.FechaCreacion)
                .ToList(),
            OrdenAntiguos => principales.OrderBy(comentario => comentario.FechaCreacion).ToList(),
            _ => principales.OrderByDescending(comentario => comentario.FechaCreacion).ToList(),
        };
    }

    private static string NormalizeOrder(string? orden) =>
        orden?.Trim().ToLowerInvariant() ?? string.Empty;
}
