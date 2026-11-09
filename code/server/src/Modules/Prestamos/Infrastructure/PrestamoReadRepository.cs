using System.Globalization;
using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using PrestamoEntity = UCB_Hold.Server.Modules.Prestamos.Domain.Prestamo;
using UCB_Hold.Server.Modules.Prestamos.Application;
using UCB_Hold.Server.Modules.Prestamos.Application.State;
using UCB_Hold.Server.Modules.Prestamos.Domain;
using UCB_Hold.Server.Modules.Usuarios.Domain;
using UCB_Hold.Server.Shared.Persistence;

namespace UCB_Hold.Server.Modules.Prestamos.Infrastructure;

public sealed class PrestamoReadRepository
{
    public const int MaxPageSize = 500;
    private readonly ApplicationDbContext _dbContext;

    public PrestamoReadRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<List<PrestamoDto>>> GetAll(
        CancellationToken cancellationToken = default
    ) => Result<List<PrestamoDto>>.Success(
        await GetPage(null, null, 0, MaxPageSize, cancellationToken: cancellationToken)
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
        bool? guardado = null,
        CancellationToken cancellationToken = default
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
                guardado,
                cancellationToken
            )
        );
    }

    public async Task<List<PrestamoDto>> GetPage(
        string? carnetUsuario,
        EstadoPrestamo? estado,
        int offset,
        int limit,
        bool? guardado = null,
        CancellationToken cancellationToken = default
    )
    {
        var pageIds = await OrderedIds(Filter(carnetUsuario, estado, guardado))
            .Skip(Math.Max(0, offset))
            .Take(Math.Clamp(limit, 1, MaxPageSize))
            .ToListAsync(cancellationToken);

        if (pageIds.Count == 0)
            return [];

        var position = pageIds
            .Select((id, index) => (id, index))
            .ToDictionary(item => item.id, item => item.index);
        var loans = await GetPrestamoList(
            _dbContext.Prestamos.AsNoTracking().Where(loan => pageIds.Contains(loan.Id)),
            cancellationToken
        );
        return loans.OrderBy(loan => position[loan.Id!.Value]).ToList();
    }

    private IQueryable<PrestamoEntity> Filter(
        string? carnetUsuario,
        EstadoPrestamo? estado,
        bool? guardado
    )
    {
        var query = _dbContext.Prestamos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(carnetUsuario))
            query = query.Where(loan => loan.Carnet == carnetUsuario);

        if (estado.HasValue)
            query = query.Where(loan => loan.EstadoPrestamo == estado.Value);

        if (guardado.HasValue)
            query = query.Where(loan => loan.Guardado == guardado.Value);

        return query;
    }

    private IQueryable<int> OrderedIds(IQueryable<PrestamoEntity> query) =>
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
        select loan.Id;

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
        var rows = (await LoanRows(source).ToListAsync(cancellationToken))
            .OrderBy(row => row.DetalleId)
            .ToList();
        var equipmentIds = rows
            .Where(row => row.IdEquipo.HasValue)
            .Select(row => row.IdEquipo!.Value)
            .Distinct()
            .ToList();
        var locations = equipmentIds.Count == 0
            ? new Dictionary<int, EquipmentLocation>()
            : await EquipmentLocations(equipmentIds, cancellationToken);

        return rows
            .Select(row => ToDto(
                row,
                row.IdEquipo.HasValue ? locations.GetValueOrDefault(row.IdEquipo.Value) : null
            ))
            .ToList();
    }

    private IQueryable<PrestamoRow> LoanRows(IQueryable<PrestamoEntity> source) =>
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
        join grupo in _dbContext.GruposEquipos.AsNoTracking().IgnoreQueryFilters()
            on detalle.IdGrupoEquipo equals grupo.Id
            into grupoJoin
        from grupo in grupoJoin.DefaultIfEmpty()
        select new PrestamoRow
        {
            Prestamo = prestamo,
            UsuarioNombre = usuario != null ? usuario.Nombre : null,
            UsuarioApellido = usuario != null ? usuario.ApellidoPaterno : null,
            UsuarioTelefono = usuario != null ? usuario.Telefono : null,
            TipoUsuario = usuario != null ? usuario.Rol.ToString() : null,
            DetalleId = detalle != null ? (int?)detalle.Id : null,
            GrupoEquipoId = detalle != null ? (int?)detalle.IdGrupoEquipo : null,
            NombreGrupoEquipo = grupo != null ? grupo.Nombre : null,
            IdEquipo = equipo != null ? (int?)equipo.Id : null,
            CodigoImt = equipo != null ? (int?)equipo.CodigoImt : null,
            CodigoUcb = equipo != null ? equipo.CodigoUcb : null,
            NumeroSerial = equipo != null ? equipo.NumeroSerial : null,
        };

    private async Task<Dictionary<int, EquipmentLocation>> EquipmentLocations(
        List<int> equipmentIds,
        CancellationToken cancellationToken
    ) => await (
        from equipo in _dbContext.Equipos.AsNoTracking().IgnoreQueryFilters()
        where equipmentIds.Contains(equipo.Id)
        join gavetero in _dbContext.Gaveteros.AsNoTracking().IgnoreQueryFilters()
            on equipo.IdGavetero equals gavetero.Id
            into gaveteroJoin
        from gavetero in gaveteroJoin.DefaultIfEmpty()
        join mueble in _dbContext.Muebles.AsNoTracking().IgnoreQueryFilters()
            on gavetero.IdMueble equals mueble.Id
            into muebleJoin
        from mueble in muebleJoin.DefaultIfEmpty()
        join ambiente in _dbContext.Ambientes.AsNoTracking().IgnoreQueryFilters()
            on (mueble != null ? mueble.IdAmbiente : null) ?? equipo.IdAmbiente equals ambiente.Id
            into ambienteJoin
        from ambiente in ambienteJoin.DefaultIfEmpty()
        join responsable in _dbContext.Usuarios.AsNoTracking().IgnoreQueryFilters()
            on (ambiente != null ? ambiente.CarnetAdministrador : null) equals responsable.Carnet
            into responsableJoin
        from responsable in responsableJoin.DefaultIfEmpty()
        select new EquipmentLocation(
            equipo.Id,
            ambiente != null ? ambiente.Nombre : null,
            responsable != null
                ? (responsable.Nombre + " " + responsable.ApellidoPaterno + " " + responsable.ApellidoMaterno).Trim()
                : null,
            gavetero != null ? gavetero.Nombre : null,
            mueble != null ? mueble.Nombre : null,
            mueble != null ? mueble.Ubicacion : null
        )
    ).ToDictionaryAsync(location => location.IdEquipo, cancellationToken);

    private static PrestamoDto ToDto(PrestamoRow row, EquipmentLocation? location) =>
        new()
        {
            Id = row.Prestamo.Id,
            CarnetUsuario = row.Prestamo.Carnet,
            NombreUsuario = row.UsuarioNombre,
            ApellidoPaternoUsuario = row.UsuarioApellido,
            TelefonoUsuario = row.UsuarioTelefono,
            TipoUsuario = row.TipoUsuario,
            EstadoPrestamo = PrestamoState.ToText(row.Prestamo.EstadoPrestamo),
            FechaSolicitud = row.Prestamo.FechaSolicitud,
            FechaPrestamoEsperada = row.Prestamo.FechaPrestamoEsperada,
            FechaPrestamo = row.Prestamo.FechaPrestamo,
            FechaDevolucionEsperada = row.Prestamo.FechaDevolucionEsperada,
            FechaDevolucion = row.Prestamo.FechaDevolucion,
            Observacion = row.Prestamo.Observacion,
            MotivoRechazo = row.Prestamo.MotivoRechazo,
            AutorizadoPor = row.Prestamo.AutorizadoPor,
            EntregadoPor = row.Prestamo.EntregadoPor,
            IdContrato = row.Prestamo.IdContrato,
            DestinoPrestamo = row.Prestamo.DestinoPrestamo,
            IdCarrera = row.Prestamo.IdCarrera,
            NombreMateria = row.Prestamo.NombreMateria,
            Guardado = row.Prestamo.Guardado,
            GrupoEquipoId = row.GrupoEquipoId.HasValue ? [row.GrupoEquipoId.Value] : [],
            NombreGrupoEquipo = row.NombreGrupoEquipo,
            CodigoImt = row.CodigoImt?.ToString(CultureInfo.InvariantCulture),
            CodigoUcb = row.CodigoUcb,
            NumeroSerial = row.NumeroSerial,
            UbicacionEquipo = location?.Ambiente,
            NombreGavetero = location?.Gavetero,
            NombreMueble = location?.Mueble,
            UbicacionMueble = location?.UbicacionMueble,
            AdministradorAmbiente = location?.Administrador,
        };

    private sealed class PrestamoRow
    {
        public required PrestamoEntity Prestamo { get; init; }
        public string? UsuarioNombre { get; init; }
        public string? UsuarioApellido { get; init; }
        public string? UsuarioTelefono { get; init; }
        public string? TipoUsuario { get; init; }
        public int? DetalleId { get; init; }
        public int? GrupoEquipoId { get; init; }
        public string? NombreGrupoEquipo { get; init; }
        public int? IdEquipo { get; init; }
        public int? CodigoImt { get; init; }
        public string? CodigoUcb { get; init; }
        public string? NumeroSerial { get; init; }
    }

    private sealed record EquipmentLocation(
        int IdEquipo,
        string? Ambiente,
        string? Administrador,
        string? Gavetero,
        string? Mueble,
        string? UbicacionMueble
    );
}
