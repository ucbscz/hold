using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Usuario;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using UsuarioEntity = IMT_Reservas.Server.Core.Entities.Usuario;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class UsuarioRepository : Repository<UsuarioEntity, UsuarioDto>
{
    private readonly PrestamoRepository _prestamoRepository;

    public UsuarioRepository(
        ApplicationDbContext dbContext,
        UsuarioMapper mapper,
        PrestamoRepository prestamoRepository
    )
        : base(dbContext, mapper) => _prestamoRepository = prestamoRepository;

    public override async Task<Result<List<UsuarioDto>>> GetAll()
    {
        var dtos = await DbContext
            .Usuarios.AsNoTracking()
            .Join(
                DbContext.Carreras,
                usuario => usuario.IdCarrera,
                carrera => carrera.Id,
                (usuario, carrera) =>
                    new UsuarioDto
                    {
                        Carnet = usuario.Carnet,
                        Nombre = usuario.Nombre,
                        ApellidoPaterno = usuario.ApellidoPaterno,
                        ApellidoMaterno = usuario.ApellidoMaterno,
                        Rol = usuario.Rol.ToString().ToLowerInvariant(),
                        Email = usuario.Email,
                        CarreraNombre = carrera.Nombre,
                        IdCarrera = usuario.IdCarrera,
                        Telefono = usuario.Telefono,
                        TelefonoReferencia = usuario.TelefonoReferencia,
                        NombreReferencia = usuario.NombreReferencia,
                        EmailReferencia = usuario.EmailReferencia,
                        Bloqueado = usuario.Bloqueado,
                        MotivoBloqueo = usuario.MotivoBloqueo,
                    }
            )
            .ToListAsync();

        return Result<List<UsuarioDto>>.Success(dtos);
    }

    public async Task<UsuarioEntity?> GetByCarnet(string carnet) =>
        await DbContext
            .Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Carnet == carnet && !u.EstadoEliminado);

    public async Task<UsuarioEntity?> GetTrackedByCarnet(string carnet) =>
        await DbContext.Usuarios.FirstOrDefaultAsync(u => u.Carnet == carnet && !u.EstadoEliminado);

    public async Task<string?> GetDisplayName(string carnet)
    {
        var user = await DbContext
            .Usuarios.AsNoTracking()
            .Where(user => user.Carnet == carnet && !user.EstadoEliminado)
            .Select(user => new
            {
                user.Nombre,
                user.ApellidoPaterno,
                user.ApellidoMaterno,
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return null;

        return string.Join(
            " ",
            new[] { user.Nombre, user.ApellidoPaterno, user.ApellidoMaterno }.Where(part =>
                !string.IsNullOrWhiteSpace(part)
            )
        );
    }

    public async Task<Result<object>> Delete(string carnet)
    {
        var entity = await DbContext.Usuarios.FirstOrDefaultAsync(u =>
            u.Carnet == carnet && !u.EstadoEliminado
        );

        if (entity == null)
            return Result<object>.NotFound();

        await SoftDelete(entity);
        await DbContext.SaveChangesAsync();

        return Result<object>.Success(null!);
    }

    protected override async Task CascadeDelete(UsuarioEntity user)
    {
        var loans = await DbContext
            .Prestamos.Where(loan => loan.Carnet == user.Carnet)
            .ToListAsync();

        foreach (var loan in loans)
        {
            loan.EstadoPrestamo = EstadoPrestamo.Cancelado;
            await _prestamoRepository.SoftDelete(loan);
        }
    }

    public async Task<bool> ExistsByCarnet(string carnet) =>
        await DbContext.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Carnet == carnet);

    public async Task<bool> ExistsByEmail(string email) =>
        await DbContext.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email == email);

    public async Task<bool> ExistsByTelefono(string telefono, string? excludeCarnet = null) =>
        await DbContext
            .Usuarios.IgnoreQueryFilters()
            .AnyAsync(u => u.Telefono == telefono && u.Carnet != excludeCarnet);

    public async Task<int?> FindCarreraIdByName(string name) =>
        await DbContext
            .Carreras.AsNoTracking()
            .Where(c => c.Nombre == name && !c.EstadoEliminado)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync();

    public async Task<string?> GetCarreraName(int idCarrera) =>
        await DbContext
            .Carreras.AsNoTracking()
            .Where(c => c.Id == idCarrera)
            .Select(c => c.Nombre)
            .FirstOrDefaultAsync();

    public async Task<(UsuarioEntity? Usuario, string? CarreraNombre)> GetByEmailWithCarrera(
        string email
    )
    {
        var result = await DbContext
            .Usuarios.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => u.Email == email && !u.EstadoEliminado)
            .Join(
                DbContext.Carreras.Where(c => !c.EstadoEliminado),
                u => u.IdCarrera,
                c => c.Id,
                (u, c) =>
                    new
                    {
                        u.Carnet,
                        u.Nombre,
                        u.ApellidoPaterno,
                        u.ApellidoMaterno,
                        u.Email,
                        u.Contrasena,
                        u.Rol,
                        u.Telefono,
                        u.TelefonoReferencia,
                        u.NombreReferencia,
                        u.EmailReferencia,
                        u.IdCarrera,
                        u.EstadoEliminado,
                        u.RefreshToken,
                        u.RefreshTokenExpiry,
                        CarreraNombre = c.Nombre,
                    }
            )
            .FirstOrDefaultAsync();

        if (result == null)
            return (null, null);

        var entity = new UsuarioEntity
        {
            Carnet = result.Carnet,
            Nombre = result.Nombre,
            ApellidoPaterno = result.ApellidoPaterno,
            ApellidoMaterno = result.ApellidoMaterno,
            Email = result.Email,
            Contrasena = result.Contrasena,
            Rol = result.Rol,
            Telefono = result.Telefono,
            TelefonoReferencia = result.TelefonoReferencia,
            NombreReferencia = result.NombreReferencia,
            EmailReferencia = result.EmailReferencia,
            IdCarrera = result.IdCarrera,
            EstadoEliminado = result.EstadoEliminado,
            RefreshToken = result.RefreshToken,
            RefreshTokenExpiry = result.RefreshTokenExpiry,
        };

        return (entity, result.CarreraNombre);
    }

    public Task UpdateEntity(UsuarioEntity entity, bool saveChanges = true)
    {
        DbContext.Usuarios.Update(entity);

        return saveChanges ? DbContext.SaveChangesAsync() : Task.CompletedTask;
    }

    public Task SaveChanges() => DbContext.SaveChangesAsync();

    public async Task SetBlockedStatus(
        IReadOnlyCollection<string> carnets,
        bool isBlocked,
        string? reason
    )
    {
        if (carnets.Count == 0)
            return;

        await DbContext
            .Usuarios.Where(user => carnets.Contains(user.Carnet))
            .ExecuteUpdateAsync(update =>
                update
                    .SetProperty(user => user.Bloqueado, isBlocked)
                    .SetProperty(user => user.MotivoBloqueo, isBlocked ? reason : null)
            );
    }

    public async Task<(UsuarioEntity? Usuario, string? CarreraNombre)> GetByRefreshTokenWithCarrera(
        string token
    )
    {
        var result = await DbContext
            .Usuarios.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => u.RefreshToken == token)
            .Join(
                DbContext.Carreras.Where(c => !c.EstadoEliminado),
                u => u.IdCarrera,
                c => c.Id,
                (u, c) =>
                    new
                    {
                        u.Carnet,
                        u.Nombre,
                        u.ApellidoPaterno,
                        u.ApellidoMaterno,
                        u.Email,
                        u.Contrasena,
                        u.Rol,
                        u.Telefono,
                        u.TelefonoReferencia,
                        u.NombreReferencia,
                        u.EmailReferencia,
                        u.IdCarrera,
                        u.EstadoEliminado,
                        u.RefreshToken,
                        u.RefreshTokenExpiry,
                        CarreraNombre = c.Nombre,
                    }
            )
            .FirstOrDefaultAsync();

        if (result == null)
            return (null, null);

        var entity = new UsuarioEntity
        {
            Carnet = result.Carnet,
            Nombre = result.Nombre,
            ApellidoPaterno = result.ApellidoPaterno,
            ApellidoMaterno = result.ApellidoMaterno,
            Email = result.Email,
            Contrasena = result.Contrasena,
            Rol = result.Rol,
            Telefono = result.Telefono,
            TelefonoReferencia = result.TelefonoReferencia,
            NombreReferencia = result.NombreReferencia,
            EmailReferencia = result.EmailReferencia,
            IdCarrera = result.IdCarrera,
            EstadoEliminado = result.EstadoEliminado,
            RefreshToken = result.RefreshToken,
            RefreshTokenExpiry = result.RefreshTokenExpiry,
        };

        return (entity, result.CarreraNombre);
    }

    public async Task UpdateRefreshToken(string carnet, string? token, DateTime? expiry)
    {
        var entity = await DbContext
            .Usuarios.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Carnet == carnet);

        if (entity == null)
            return;

        entity.RefreshToken = token;
        entity.RefreshTokenExpiry = expiry;
        await DbContext.SaveChangesAsync();
    }
}
