using System.Globalization;
using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Contrato;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Application.Features.Prestamo.State;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;
using PrestamoEntity = IMT_Reservas.Server.Core.Entities.Prestamo;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public sealed class PrestamoReadRepository
{
    public const int MaxPageSize = 500;
    private readonly ApplicationDbContext _dbContext;

    public PrestamoReadRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<List<PrestamoDto>>> GetAll(
        CancellationToken cancellationToken = default
    ) => Result<List<PrestamoDto>>.Success(
        await GetPage(null, null, 0, MaxPageSize, cancellationToken)
    );

    public async Task<Result<PrestamoDto>> Get(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var list = await GetPrestamoList(
            _dbContext.Prestamos.AsNoTracking().Where(loan => loan.Id == id),
            cancellationToken
        );
        var item = list.FirstOrDefault();
        return item == null
            ? Result<PrestamoDto>.NotFound()
            : Result<PrestamoDto>.Success(item);
    }

    public async Task<Result<PrestamoDto>> GetAuthorized(
        int id,
        string carnet,
        bool isAdmin,
        CancellationToken cancellationToken = default
    )
    {
        var query = _dbContext.Prestamos.AsNoTracking().Where(loan => loan.Id == id);

        if (!isAdmin)
            query = query.Where(loan => loan.Carnet == carnet);

        var item = (await GetPrestamoList(query, cancellationToken)).FirstOrDefault();
        return item == null
            ? Result<PrestamoDto>.NotFound()
            : Result<PrestamoDto>.Success(item);
    }

    public async Task<Result<List<PrestamoDto>>> GetHistoryWithDetails(
        string? carnetUsuario,
        EstadoPrestamo? estado,
        int page = 1,
        int pageSize = MaxPageSize,
        CancellationToken cancellationToken = default,
        bool? guardado = null
    )
    {
        var safePageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        var safePage = Math.Clamp(page, 1, int.MaxValue / safePageSize);
        return Result<List<PrestamoDto>>.Success(
            await GetPage(
                carnetUsuario,
                estado,
                (safePage - 1) * safePageSize,
                safePageSize,
                cancellationToken,
                guardado
            )
        );
    }

    public async Task<List<PrestamoDto>> GetPage(
        string? carnetUsuario,
        EstadoPrestamo? estado,
        int offset,
        int limit,
        CancellationToken cancellationToken = default,
        bool? guardado = null
    )
    {
        var safeOffset = Math.Max(0, offset);
        var safeLimit = Math.Clamp(limit, 1, MaxPageSize);
        var query = _dbContext.Prestamos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(carnetUsuario))
            query = query.Where(loan => loan.Carnet == carnetUsuario);

        if (estado.HasValue)
            query = query.Where(loan => loan.EstadoPrestamo == estado.Value);

        if (guardado.HasValue)
            query = query.Where(loan => loan.Guardado == guardado.Value);

        var pageIds = await (
            from loan in query
            join user in _dbContext.Usuarios.AsNoTracking().IgnoreQueryFilters()
                on loan.Carnet equals user.Carnet
                into userJoin
            from user in userJoin.DefaultIfEmpty()
            orderby
                loan.EstadoPrestamo == EstadoPrestamo.Pendiente ? 0 : 1,
                (loan.EstadoPrestamo == EstadoPrestamo.Finalizado
                    || loan.EstadoPrestamo == EstadoPrestamo.Cancelado
                    || loan.EstadoPrestamo == EstadoPrestamo.Rechazado) ? 1 : 0,
                user != null && user.Rol == TipoUsuario.Administrador ? 0
                    : user != null && user.Rol == TipoUsuario.Administrador_Laboratorio ? 1
                    : user != null && user.Rol == TipoUsuario.Docente ? 2
                    : user != null && user.Rol == TipoUsuario.Administrativo ? 3
                    : 4,
                loan.FechaSolicitud descending,
                loan.Id descending
            select loan.Id
        )
            .Skip(safeOffset)
            .Take(safeLimit)
            .ToListAsync(cancellationToken);

        if (pageIds.Count == 0)
            return [];

        return await GetPrestamoList(
            _dbContext.Prestamos.AsNoTracking().Where(loan => pageIds.Contains(loan.Id)),
            cancellationToken
        );
    }

    public async Task<string> GetUsuarioDisplayName(
        string carnet,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _dbContext
            .Usuarios.AsNoTracking()
            .Where(usuario => usuario.Carnet == carnet)
            .Select(usuario => new
            {
                usuario.Nombre,
                usuario.ApellidoPaterno,
                usuario.ApellidoMaterno,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
            return carnet;

        var fullName = string.Join(
            " ",
            new[] { user.Nombre, user.ApellidoPaterno, user.ApellidoMaterno }.Where(part =>
                !string.IsNullOrWhiteSpace(part)
            )
        );

        return string.IsNullOrWhiteSpace(fullName) ? carnet : fullName;
    }

    public async Task<List<ContractEquipmentData>> GetContractEquipment(
        int prestamoId,
        CancellationToken cancellationToken = default
    ) => await (
        from detail in _dbContext.DetallesPrestamos.AsNoTracking()
        join equipment in _dbContext.Equipos.AsNoTracking()
            on detail.IdEquipo equals equipment.Id
        where
            detail.IdPrestamo == prestamoId
            && !detail.EstadoEliminado
            && detail.IdEquipo != null
        orderby equipment.CodigoImt
        select new ContractEquipmentData(
            detail.IdGrupoEquipo,
            equipment.CodigoImt,
            equipment.CodigoUcb,
            equipment.NumeroSerial
        )
    ).ToListAsync(cancellationToken);

    public async Task<string?> GetGrupoEquipoNombre(
        int grupoEquipoId,
        CancellationToken cancellationToken = default
    ) => await _dbContext
        .GruposEquipos.AsNoTracking()
        .Where(group => group.Id == grupoEquipoId && !group.EstadoEliminado)
        .Select(group => group.Nombre)
        .FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> HasAtrasadoPrestamo(
        string carnet,
        CancellationToken cancellationToken = default
    ) => await _dbContext.Prestamos.AnyAsync(
        loan =>
            loan.Carnet == carnet
            && loan.EstadoPrestamo == EstadoPrestamo.Atrasado
            && !loan.EstadoEliminado,
        cancellationToken
    );

    public async Task<bool> IsUserBlocked(
        string carnet,
        CancellationToken cancellationToken = default
    ) => await _dbContext.Usuarios.AnyAsync(
        user => user.Carnet == carnet && user.Bloqueado,
        cancellationToken
    );

    public async Task<string?> GetBlockReason(
        string carnet,
        CancellationToken cancellationToken = default
    ) => await _dbContext
        .Usuarios.Where(user => user.Carnet == carnet)
        .Select(user => user.MotivoBloqueo)
        .FirstOrDefaultAsync(cancellationToken);

    private async Task<List<PrestamoDto>> GetPrestamoList(
        IQueryable<PrestamoEntity> source,
        CancellationToken cancellationToken
    )
    {
        var rows = await (
            from prestamo in source
            join usuario in _dbContext.Usuarios.AsNoTracking().IgnoreQueryFilters()
                on prestamo.Carnet equals usuario.Carnet
                into usuarioJoin
            from usuario in usuarioJoin.DefaultIfEmpty()
            join detalle in _dbContext
                .DetallesPrestamos.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(detail => !detail.EstadoEliminado)
                on prestamo.Id equals detalle.IdPrestamo
                into detalleJoin
            from detalle in detalleJoin.DefaultIfEmpty()
            join equipo in _dbContext.Equipos.AsNoTracking().IgnoreQueryFilters()
                on detalle.IdEquipo equals equipo.Id
                into equipoJoin
            from equipo in equipoJoin.DefaultIfEmpty()
            join grupoReserva in _dbContext.GruposEquipos.AsNoTracking().IgnoreQueryFilters()
                on detalle.IdGrupoEquipo equals grupoReserva.Id
                into grupoReservaJoin
            from grupoReserva in grupoReservaJoin.DefaultIfEmpty()
            join gavetero in _dbContext.Gaveteros.AsNoTracking().IgnoreQueryFilters()
                on equipo.IdGavetero equals gavetero.Id
                into gaveteroJoin
            from gavetero in gaveteroJoin.DefaultIfEmpty()
            join mueble in _dbContext.Muebles.AsNoTracking().IgnoreQueryFilters()
                on gavetero.IdMueble equals mueble.Id
                into muebleJoin
            from mueble in muebleJoin.DefaultIfEmpty()
            join ambiente in _dbContext.Ambientes.AsNoTracking().IgnoreQueryFilters()
                on ((mueble != null ? mueble.IdAmbiente : null) ?? (equipo != null ? equipo.IdAmbiente : null)) equals ambiente.Id
                into ambienteJoin
            from ambiente in ambienteJoin.DefaultIfEmpty()
            join responsable in _dbContext.Usuarios.AsNoTracking().IgnoreQueryFilters()
                on (ambiente != null ? ambiente.CarnetAdministrador : null) equals responsable.Carnet
                into responsableJoin
            from responsable in responsableJoin.DefaultIfEmpty()
            orderby
                prestamo.EstadoPrestamo == EstadoPrestamo.Pendiente ? 0 : 1,
                (prestamo.EstadoPrestamo == EstadoPrestamo.Finalizado
                    || prestamo.EstadoPrestamo == EstadoPrestamo.Cancelado
                    || prestamo.EstadoPrestamo == EstadoPrestamo.Rechazado) ? 1 : 0,
                usuario != null && usuario.Rol == TipoUsuario.Administrador ? 0
                    : usuario != null && usuario.Rol == TipoUsuario.Administrador_Laboratorio ? 1
                    : usuario != null && usuario.Rol == TipoUsuario.Docente ? 2
                    : usuario != null && usuario.Rol == TipoUsuario.Administrativo ? 3
                    : 4,
                prestamo.FechaSolicitud descending,
                prestamo.Id descending
            select new
            {
                PrestamoId = prestamo.Id,
                prestamo.Carnet,
                UsuarioNombre = usuario != null ? usuario.Nombre : null,
                UsuarioApellido = usuario != null ? usuario.ApellidoPaterno : null,
                UsuarioTelefono = usuario != null ? usuario.Telefono : null,
                TipoUsuario = usuario != null ? usuario.Rol.ToString() : null,
                prestamo.EstadoPrestamo,
                prestamo.FechaSolicitud,
                prestamo.FechaPrestamoEsperada,
                prestamo.FechaPrestamo,
                prestamo.FechaDevolucionEsperada,
                prestamo.FechaDevolucion,
                prestamo.Observacion,
                prestamo.MotivoRechazo,
                prestamo.AutorizadoPor,
                prestamo.EntregadoPor,
                prestamo.IdContrato,
                prestamo.DestinoPrestamo,
                prestamo.IdCarrera,
                prestamo.NombreMateria,
                prestamo.Guardado,
                GrupoEquipoId = detalle != null ? (int?)detalle.IdGrupoEquipo : null,
                NombreGrupoEquipo = grupoReserva != null ? grupoReserva.Nombre : null,
                CodigoImt = equipo != null ? (int?)equipo.CodigoImt : null,
                CodigoUcb = equipo != null ? equipo.CodigoUcb : null,
                NumeroSerial = equipo != null ? equipo.NumeroSerial : null,
                UbicacionEquipo = ambiente != null ? ambiente.Nombre : null,
                AdministradorAmbiente = responsable != null
                    ? (responsable.Nombre + " " + responsable.ApellidoPaterno + " " + responsable.ApellidoMaterno).Trim() : null,
                NombreGavetero = gavetero != null ? gavetero.Nombre : null,
                NombreMueble = mueble != null ? mueble.Nombre : null,
                UbicacionMueble = mueble != null ? mueble.Ubicacion : null,
            }
        ).ToListAsync(cancellationToken);

        return rows.Select(row => new PrestamoDto
        {
            Id = row.PrestamoId,
            CarnetUsuario = row.Carnet,
            NombreUsuario = row.UsuarioNombre,
            ApellidoPaternoUsuario = row.UsuarioApellido,
            TelefonoUsuario = row.UsuarioTelefono,
            TipoUsuario = row.TipoUsuario,
            EstadoPrestamo = PrestamoState.ToText(row.EstadoPrestamo),
            FechaSolicitud = row.FechaSolicitud,
            FechaPrestamoEsperada = row.FechaPrestamoEsperada,
            FechaPrestamo = row.FechaPrestamo,
            FechaDevolucionEsperada = row.FechaDevolucionEsperada,
            FechaDevolucion = row.FechaDevolucion,
            Observacion = row.Observacion,
            MotivoRechazo = row.MotivoRechazo,
            AutorizadoPor = row.AutorizadoPor,
            EntregadoPor = row.EntregadoPor,
            IdContrato = row.IdContrato,
            DestinoPrestamo = row.DestinoPrestamo,
            IdCarrera = row.IdCarrera,
            NombreMateria = row.NombreMateria,
            Guardado = row.Guardado,
            GrupoEquipoId = row.GrupoEquipoId.HasValue ? [row.GrupoEquipoId.Value] : [],
            NombreGrupoEquipo = row.NombreGrupoEquipo,
            CodigoImt = row.CodigoImt?.ToString(CultureInfo.InvariantCulture),
            CodigoUcb = row.CodigoUcb,
            NumeroSerial = row.NumeroSerial,
            UbicacionEquipo = row.UbicacionEquipo,
            NombreGavetero = row.NombreGavetero,
            NombreMueble = row.NombreMueble,
            UbicacionMueble = row.UbicacionMueble,
            AdministradorAmbiente = row.AdministradorAmbiente,
        }).ToList();
    }
}
