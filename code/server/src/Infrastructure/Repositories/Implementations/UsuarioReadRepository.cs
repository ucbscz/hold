using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Usuario;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;
using UsuarioEntity = IMT_Reservas.Server.Core.Entities.Usuario;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public sealed class UsuarioReadRepository
{
    public const int MaxPageSize = 500;
    private readonly ApplicationDbContext _dbContext;

    public UsuarioReadRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<List<UsuarioDto>>> GetAll(
        CancellationToken cancellationToken = default
    ) => await GetPage(1, MaxPageSize, cancellationToken);

    public async Task<Result<List<UsuarioDto>>> GetPage(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        var safePageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var safePage = Math.Clamp(page, 1, int.MaxValue / safePageSize);
        var users = _dbContext
            .Usuarios.AsNoTracking()
            .OrderBy(user => user.Carnet)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize);
        var dtos = await users
            .Join(
                _dbContext.Carreras,
                user => user.IdCarrera,
                career => career.Id,
                (user, career) => new UsuarioDto
                {
                    Carnet = user.Carnet,
                    Nombre = user.Nombre,
                    ApellidoPaterno = user.ApellidoPaterno,
                    ApellidoMaterno = user.ApellidoMaterno,
                    Rol = user.Rol.ToString().ToLowerInvariant(),
                    Email = user.Email,
                    CarreraNombre = career.Nombre,
                    IdCarrera = user.IdCarrera,
                    Telefono = user.Telefono,
                    TelefonoReferencia = user.TelefonoReferencia,
                    NombreReferencia = user.NombreReferencia,
                    EmailReferencia = user.EmailReferencia,
                    Bloqueado = user.Bloqueado,
                    MotivoBloqueo = user.MotivoBloqueo,
                }
            )
            .ToListAsync(cancellationToken);

        return Result<List<UsuarioDto>>.Success(dtos);
    }

    public Task<UsuarioEntity?> GetByCarnet(
        string carnet,
        CancellationToken cancellationToken = default
    ) => _dbContext
        .Usuarios.AsNoTracking()
        .FirstOrDefaultAsync(
            user => user.Carnet == carnet && !user.EstadoEliminado,
            cancellationToken
        );

    public async Task<string?> GetDisplayName(
        string carnet,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _dbContext
            .Usuarios.AsNoTracking()
            .Where(item => item.Carnet == carnet && !item.EstadoEliminado)
            .Select(item => new
            {
                item.Nombre,
                item.ApellidoPaterno,
                item.ApellidoMaterno,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            return null;

        return string.Join(
            " ",
            new[] { user.Nombre, user.ApellidoPaterno, user.ApellidoMaterno }.Where(part =>
                !string.IsNullOrWhiteSpace(part)
            )
        );
    }

    public Task<bool> ExistsByCarnet(
        string carnet,
        CancellationToken cancellationToken = default
    ) => _dbContext.Usuarios.IgnoreQueryFilters().AnyAsync(
        user => user.Carnet == carnet,
        cancellationToken
    );

    public Task<bool> ExistsByEmail(
        string email,
        string? excludeCarnet = null,
        CancellationToken cancellationToken = default
    ) => _dbContext.Usuarios.IgnoreQueryFilters().AnyAsync(
        user => user.Email == email && user.Carnet != excludeCarnet,
        cancellationToken
    );

    public Task<bool> ExistsByTelefono(
        string telefono,
        string? excludeCarnet = null,
        CancellationToken cancellationToken = default
    ) => _dbContext
        .Usuarios.IgnoreQueryFilters()
        .AnyAsync(
            user => user.Telefono == telefono && user.Carnet != excludeCarnet,
            cancellationToken
        );

    public Task<int?> FindCarreraIdByName(
        string name,
        CancellationToken cancellationToken = default
    ) => _dbContext
        .Carreras.AsNoTracking()
        .Where(career => career.Nombre == name && !career.EstadoEliminado)
        .Select(career => (int?)career.Id)
        .FirstOrDefaultAsync(cancellationToken);

    public Task<string?> GetCarreraName(
        int idCarrera,
        CancellationToken cancellationToken = default
    ) => _dbContext
        .Carreras.AsNoTracking()
        .Where(career => career.Id == idCarrera)
        .Select(career => career.Nombre)
        .FirstOrDefaultAsync(cancellationToken);

    public Task<List<string>> GetUnblockedCarnets(
        IReadOnlyCollection<string> carnets,
        CancellationToken cancellationToken = default
    ) => _dbContext
        .Usuarios.AsNoTracking()
        .Where(user => carnets.Contains(user.Carnet) && !user.Bloqueado)
        .Select(user => user.Carnet)
        .ToListAsync(cancellationToken);
}
