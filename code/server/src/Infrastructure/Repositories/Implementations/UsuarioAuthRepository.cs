using System.Linq.Expressions;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;
using UsuarioEntity = IMT_Reservas.Server.Core.Entities.Usuario;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public sealed class UsuarioAuthRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UsuarioAuthRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<(UsuarioEntity? Usuario, string? CarreraNombre)> GetByEmailWithCarrera(
        string email,
        CancellationToken cancellationToken = default
    ) =>
        Map(
            await GetActiveUser(
                user => user.Email == email,
                cancellationToken
            )
        );

    public async Task<(UsuarioEntity? Usuario, string? CarreraNombre)> GetByRefreshTokenWithCarrera(
        string token,
        CancellationToken cancellationToken = default
    ) =>
        Map(
            await GetActiveUser(
                user => user.RefreshToken == token,
                cancellationToken
            )
        );

    public async Task UpdateRefreshToken(
        string carnet,
        string? token,
        DateTime? expiry,
        CancellationToken cancellationToken = default
    )
    {
        var entity = await _dbContext
            .Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                user => user.Carnet == carnet && !user.EstadoEliminado,
                cancellationToken
            );

        if (entity == null)
            return;

        entity.RefreshToken = token;
        entity.RefreshTokenExpiry = expiry;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<UsuarioEntity?> GetTrackedByEmail(
        string email,
        CancellationToken cancellationToken = default
    ) => _dbContext.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(
        user => user.Email == email && !user.EstadoEliminado,
        cancellationToken
    );

    public async Task SaveVerification(
        UsuarioEntity user,
        CancellationToken cancellationToken = default
    )
    {
        _dbContext.Usuarios.Update(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ConfirmEmail(
        string tokenHash,
        CancellationToken cancellationToken = default
    )
    {
        if (!_dbContext.Database.IsRelational())
        {
            var user = await _dbContext.Usuarios.FirstOrDefaultAsync(
                item => !item.EmailVerificado
                    && item.TokenVerificacionHash == tokenHash
                    && item.TokenVerificacionExpira > DateTime.UtcNow,
                cancellationToken
            );
            if (user == null)
                return false;
            user.EmailVerificado = true;
            user.TokenVerificacionHash = null;
            user.TokenVerificacionExpira = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        return await _dbContext.Usuarios
            .Where(user => !user.EmailVerificado
                && user.TokenVerificacionHash == tokenHash
                && user.TokenVerificacionExpira > DateTime.UtcNow)
            .ExecuteUpdateAsync(update => update
                .SetProperty(user => user.EmailVerificado, true)
                .SetProperty(user => user.TokenVerificacionHash, (string?)null)
                .SetProperty(user => user.TokenVerificacionExpira, (DateTime?)null),
                cancellationToken) == 1;
    }

    public async Task LinkGoogle(
        string carnet,
        string googleId,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _dbContext.Usuarios.FirstAsync(
            item => item.Carnet == carnet,
            cancellationToken
        );
        user.GoogleId = googleId;
        user.EmailVerificado = true;
        user.TokenVerificacionHash = null;
        user.TokenVerificacionExpira = null;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RotateRefreshToken(
        string carnet,
        string currentTokenHash,
        string newTokenHash,
        DateTime expiry,
        CancellationToken cancellationToken = default
    )
    {
        if (!_dbContext.Database.IsRelational())
        {
            var entity = await _dbContext
                .Usuarios.IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    user =>
                        user.Carnet == carnet
                        && !user.EstadoEliminado
                        && user.RefreshToken == currentTokenHash
                        && user.RefreshTokenExpiry >= DateTime.UtcNow,
                    cancellationToken
                );

            if (entity == null)
                return false;

            entity.RefreshToken = newTokenHash;
            entity.RefreshTokenExpiry = expiry;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        var updated = await _dbContext
            .Usuarios.IgnoreQueryFilters()
            .Where(user =>
                user.Carnet == carnet
                && !user.EstadoEliminado
                && user.RefreshToken == currentTokenHash
                && user.RefreshTokenExpiry >= DateTime.UtcNow
            )
            .ExecuteUpdateAsync(
                update =>
                    update
                        .SetProperty(user => user.RefreshToken, newTokenHash)
                        .SetProperty(user => user.RefreshTokenExpiry, expiry),
                cancellationToken
            );

        return updated == 1;
    }

    private Task<UsuarioAuthData?> GetActiveUser(
        Expression<Func<UsuarioEntity, bool>> predicate,
        CancellationToken cancellationToken
    ) =>
        _dbContext
            .Usuarios.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(user => !user.EstadoEliminado)
            .Where(predicate)
            .Join(
                _dbContext
                    .Carreras.AsNoTracking()
                    .IgnoreQueryFilters()
                    .Where(career => !career.EstadoEliminado),
                user => user.IdCarrera,
                career => career.Id,
                (user, career) =>
                    new UsuarioAuthData(
                        user.Carnet,
                        user.Nombre,
                        user.ApellidoPaterno,
                        user.ApellidoMaterno,
                        user.Email,
                        user.Contrasena,
                        user.Rol,
                        user.Telefono,
                        user.TelefonoReferencia,
                        user.NombreReferencia,
                        user.EmailReferencia,
                        user.IdCarrera,
                        user.RefreshToken,
                        user.RefreshTokenExpiry,
                        user.EmailVerificado,
                        user.GoogleId,
                        career.Nombre
                    )
            )
            .FirstOrDefaultAsync(cancellationToken);

    private static (UsuarioEntity? Usuario, string? CarreraNombre) Map(UsuarioAuthData? data)
    {
        if (data == null)
            return (null, null);

        return (
            new UsuarioEntity
            {
                Carnet = data.Carnet,
                Nombre = data.Nombre,
                ApellidoPaterno = data.ApellidoPaterno ?? string.Empty,
                ApellidoMaterno = data.ApellidoMaterno ?? string.Empty,
                Email = data.Email,
                Contrasena = data.Contrasena,
                Rol = data.Rol,
                Telefono = data.Telefono ?? string.Empty,
                TelefonoReferencia = data.TelefonoReferencia,
                NombreReferencia = data.NombreReferencia,
                EmailReferencia = data.EmailReferencia,
                IdCarrera = data.IdCarrera,
                RefreshToken = data.RefreshToken,
                RefreshTokenExpiry = data.RefreshTokenExpiry,
                EmailVerificado = data.EmailVerificado,
                GoogleId = data.GoogleId,
            },
            data.CarreraNombre
        );
    }

    private sealed record UsuarioAuthData(
        string Carnet,
        string Nombre,
        string? ApellidoPaterno,
        string? ApellidoMaterno,
        string Email,
        string Contrasena,
        TipoUsuario Rol,
        string? Telefono,
        string? TelefonoReferencia,
        string? NombreReferencia,
        string? EmailReferencia,
        int IdCarrera,
        string? RefreshToken,
        DateTime? RefreshTokenExpiry,
        bool EmailVerificado,
        string? GoogleId,
        string CarreraNombre
    );
}
