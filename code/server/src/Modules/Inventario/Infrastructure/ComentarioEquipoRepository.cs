using IMT_Reservas.Server.Application.Features.GrupoEquipo;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class ComentarioEquipoRepository
{
    private readonly ApplicationDbContext _db;

    public ComentarioEquipoRepository(ApplicationDbContext db) => _db = db;

    public async Task<bool> GrupoExiste(int grupoId) =>
        await _db.GruposEquipos.AsNoTracking().AnyAsync(grupo => grupo.Id == grupoId);

    public async Task<bool> ComentarioExiste(int comentarioId, int grupoId) =>
        await _db.ComentariosEquipos
            .AsNoTracking()
            .AnyAsync(comentario =>
                comentario.Id == comentarioId && comentario.IdGrupoEquipo == grupoId
            );

    public async Task<ComentarioEquipo?> GetComentario(int comentarioId, int grupoId) =>
        await _db.ComentariosEquipos.FirstOrDefaultAsync(comentario =>
            comentario.Id == comentarioId && comentario.IdGrupoEquipo == grupoId
        );

    public async Task<List<ComentarioEquipoDto>> GetByGrupo(
        int grupoId,
        string currentCarnet,
        bool isAdmin
    )
    {
        var rows = await QueryComentarioRows()
            .Where(comentario => comentario.IdGrupoEquipo == grupoId)
            .ToListAsync();

        return rows.Select(row => MapRow(row, currentCarnet, isAdmin)).ToList();
    }

    public async Task<ComentarioEquipoDto?> GetDto(
        int comentarioId,
        string currentCarnet,
        bool isAdmin
    )
    {
        var row = await QueryComentarioRows()
            .FirstOrDefaultAsync(comentario => comentario.Id == comentarioId);

        return row is null ? null : MapRow(row, currentCarnet, isAdmin);
    }

    public async Task<ComentarioEquipoDto> Add(
        int grupoId,
        string carnet,
        string contenido,
        int? idComentarioPadre
    )
    {
        var comentario = new ComentarioEquipo
        {
            IdGrupoEquipo = grupoId,
            IdComentarioPadre = idComentarioPadre,
            CarnetUsuario = carnet,
            Contenido = contenido,
            FechaCreacion = DateTime.UtcNow,
        };

        _db.ComentariosEquipos.Add(comentario);
        await _db.SaveChangesAsync();

        return (await GetDto(comentario.Id, carnet, false))!;
    }

    public async Task<ComentarioEquipoDto?> ToggleLike(
        int comentarioId,
        int grupoId,
        string carnet,
        bool isAdmin
    )
    {
        var comentario = await GetComentario(comentarioId, grupoId);

        if (comentario is null)
            return null;

        var likedBy = SplitLikedBy(comentario.LikedBy);

        if (!likedBy.Add(carnet))
            likedBy.Remove(carnet);

        comentario.LikedBy = string.Join(",", likedBy.Order(StringComparer.OrdinalIgnoreCase));
        comentario.Likes = likedBy.Count;

        await _db.SaveChangesAsync();

        return await GetDto(comentarioId, carnet, isAdmin);
    }

    public async Task DeleteTree(int comentarioId, int grupoId)
    {
        var comentarios = await _db.ComentariosEquipos
            .Where(comentario => comentario.IdGrupoEquipo == grupoId)
            .ToListAsync();
        var ids = new HashSet<int> { comentarioId };
        var changed = true;

        while (changed)
        {
            changed = false;

            foreach (var comentario in comentarios)
            {
                if (!comentario.IdComentarioPadre.HasValue)
                    continue;
                if (!ids.Contains(comentario.IdComentarioPadre.Value))
                    continue;
                if (!ids.Add(comentario.Id))
                    continue;

                changed = true;
            }
        }

        foreach (var comentario in comentarios.Where(comentario => ids.Contains(comentario.Id)))
            comentario.EstadoEliminado = true;

        await _db.SaveChangesAsync();
    }

    private IQueryable<ComentarioEquipoRow> QueryComentarioRows() =>
        from comentario in _db.ComentariosEquipos.AsNoTracking()
        join usuario in _db.Usuarios.AsNoTracking()
            on comentario.CarnetUsuario equals usuario.Carnet
        where !comentario.EstadoEliminado
        select new ComentarioEquipoRow
        {
            Id = comentario.Id,
            IdGrupoEquipo = comentario.IdGrupoEquipo,
            IdComentarioPadre = comentario.IdComentarioPadre,
            CarnetUsuario = comentario.CarnetUsuario,
            NombreUsuario = (usuario.Nombre + " " + usuario.ApellidoPaterno).Trim(),
            Contenido = comentario.Contenido,
            FechaCreacion = comentario.FechaCreacion,
            Likes = comentario.Likes,
            LikedBy = comentario.LikedBy,
        };

    private static ComentarioEquipoDto MapRow(
        ComentarioEquipoRow row,
        string currentCarnet,
        bool isAdmin
    ) =>
        new()
        {
            Id = row.Id,
            IdGrupoEquipo = row.IdGrupoEquipo,
            IdComentarioPadre = row.IdComentarioPadre,
            CarnetUsuario = row.CarnetUsuario,
            NombreUsuario = row.NombreUsuario,
            Contenido = row.Contenido,
            FechaCreacion = row.FechaCreacion,
            Likes = row.Likes,
            LikedByCurrentUser = HasLiked(row.LikedBy, currentCarnet),
            PuedeEliminar =
                isAdmin
                || string.Equals(row.CarnetUsuario, currentCarnet, StringComparison.OrdinalIgnoreCase),
        };

    private static bool HasLiked(string? likedBy, string currentCarnet) =>
        !string.IsNullOrWhiteSpace(currentCarnet)
        && SplitLikedBy(likedBy).Contains(currentCarnet);

    private static HashSet<string> SplitLikedBy(string? likedBy) =>
        string.IsNullOrWhiteSpace(likedBy)
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : likedBy
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private sealed class ComentarioEquipoRow
    {
        public int Id { get; init; }
        public int IdGrupoEquipo { get; init; }
        public int? IdComentarioPadre { get; init; }
        public string CarnetUsuario { get; init; } = string.Empty;
        public string NombreUsuario { get; init; } = string.Empty;
        public string Contenido { get; init; } = string.Empty;
        public DateTime FechaCreacion { get; init; }
        public int Likes { get; init; }
        public string? LikedBy { get; init; }
    }
}
