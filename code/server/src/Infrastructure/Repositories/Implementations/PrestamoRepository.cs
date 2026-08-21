using System.Data;
using System.Globalization;
using Ardalis.Result;
using IMT_Reservas.Server.Application.Features.Contrato;
using IMT_Reservas.Server.Application.Features.Prestamo;
using IMT_Reservas.Server.Application.Features.Prestamo.State;
using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using IMT_Reservas.Server.Infrastructure.Repositories.Abstraction;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PrestamoEntity = IMT_Reservas.Server.Core.Entities.Prestamo;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public class PrestamoRepository : Repository<PrestamoEntity, PrestamoDto>
{
    private readonly ContractHtmlProcessor _contractHtml;

    public PrestamoRepository(
        ApplicationDbContext dbContext,
        PrestamoMapper mapper,
        ContractHtmlProcessor contractHtml
    )
        : base(dbContext, mapper) => _contractHtml = contractHtml;

    public override async Task<Result<List<PrestamoDto>>> GetAll()
    {
        var list = await GetPrestamoList(DbContext.Prestamos.AsNoTracking());
        return Result<List<PrestamoDto>>.Success(list);
    }

    public override async Task<Result<PrestamoDto>> Get(int id)
    {
        var list = await GetPrestamoList(DbContext.Prestamos.AsNoTracking().Where(p => p.Id == id));
        var item = list.FirstOrDefault();
        return item == null ? Result<PrestamoDto>.NotFound() : Result<PrestamoDto>.Success(item);
    }

    public async Task<Result<PrestamoDto>> GetAuthorized(int id, string carnet, bool isAdmin)
    {
        var query = DbContext.Prestamos.AsNoTracking().Where(loan => loan.Id == id);

        if (!isAdmin)
            query = query.Where(loan => loan.Carnet == carnet);

        var item = (await GetPrestamoList(query)).FirstOrDefault();
        return item == null ? Result<PrestamoDto>.NotFound() : Result<PrestamoDto>.Success(item);
    }

    public async Task<Result<List<PrestamoDto>>> GetHistoryWithDetails(
        string? carnetUsuario,
        EstadoPrestamo? estado
    )
    {
        var query = DbContext.Prestamos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(carnetUsuario))
            query = query.Where(p => p.Carnet == carnetUsuario);

        if (estado.HasValue)
            query = query.Where(p => p.EstadoPrestamo == estado.Value);

        return Result<List<PrestamoDto>>.Success(await GetPrestamoList(query));
    }

    protected override async Task CascadeDelete(PrestamoEntity loan) =>
        await CascadeLeaf<DetallePrestamo>(detail => detail.IdPrestamo == loan.Id);

    public async Task<PrestamoEntity?> FindById(int id) =>
        await DbContext.Prestamos.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<string> GetUsuarioDisplayName(string carnet)
    {
        var user = await DbContext
            .Usuarios.AsNoTracking()
            .Where(usuario => usuario.Carnet == carnet)
            .Select(usuario => new
            {
                usuario.Nombre,
                usuario.ApellidoPaterno,
                usuario.ApellidoMaterno,
            })
            .FirstOrDefaultAsync();

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

    public async Task<Result<PrestamoEntity>> CreateReservation(
        PrestamoEntity entity,
        IReadOnlyCollection<int> groupIds,
        string? contractHtml
    )
    {
        await using var transaction = DbContext.Database.IsRelational()
            ? await DbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

        try
        {
            var durationViolation = await GetLoanDurationViolation(
                groupIds,
                entity.FechaPrestamoEsperada,
                entity.FechaDevolucionEsperada
            );

            if (durationViolation != null)
            {
                if (transaction != null)
                    await transaction.RollbackAsync();

                return Result<PrestamoEntity>.Error(durationViolation);
            }

            DbContext.Prestamos.Add(entity);
            await DbContext.SaveChangesAsync();

            var details = groupIds.Select(groupId => new DetallePrestamo
            {
                IdPrestamo = entity.Id,
                IdGrupoEquipo = groupId,
                EstadoEliminado = false,
            });

            DbContext.DetallesPrestamos.AddRange(details);
            await DbContext.SaveChangesAsync();

            if (!await AssignEquiposOnApproval(entity.Id))
            {
                await RollbackCreate(transaction, entity.Id);
                return Result<PrestamoEntity>.Error(
                    "No hay suficientes unidades disponibles en el horario seleccionado"
                );
            }

            if (!string.IsNullOrWhiteSpace(contractHtml))
            {
                var equipment = await GetContractEquipment(entity.Id);
                var contract = new Contrato
                {
                    ContratoHtml = _contractHtml.RenderEquipment(contractHtml, equipment),
                };

                DbContext.Contratos.Add(contract);
                await DbContext.SaveChangesAsync();

                entity.IdContrato = contract.Id;
                await DbContext.SaveChangesAsync();
            }

            if (transaction != null)
                await transaction.CommitAsync();

            return Result<PrestamoEntity>.Success(entity);
        }
        catch (ArgumentException exception)
        {
            await RollbackCreate(transaction, entity.Id);
            return Result<PrestamoEntity>.Error(exception.Message);
        }
        catch (DbUpdateException)
        {
            await RollbackCreate(transaction, entity.Id);
            return Result<PrestamoEntity>.Error(
                "La disponibilidad cambió mientras se procesaba la reserva. Intente nuevamente."
            );
        }
        catch (PostgresException exception)
            when (exception.SqlState == PostgresErrorCodes.SerializationFailure)
        {
            await RollbackCreate(transaction, entity.Id);
            return Result<PrestamoEntity>.Error(
                "Otra reserva modificó la disponibilidad. Intente nuevamente."
            );
        }
    }

    private async Task<string?> GetLoanDurationViolation(
        IReadOnlyCollection<int> groupIds,
        DateTime startDate,
        DateTime endDate
    )
    {
        var requestedIds = groupIds.Distinct().ToArray();
        var groups = await DbContext
            .GruposEquipos.AsNoTracking()
            .Where(group => requestedIds.Contains(group.Id))
            .Select(group => new
            {
                group.Nombre,
                group.TiempoMaximoPrestamoDias,
            })
            .ToListAsync();

        if (groups.Count != requestedIds.Length)
            return "Uno o más grupos de equipos no existen o no están disponibles";

        var duration = endDate - startDate;
        var exceeded = groups
            .Where(group => duration > TimeSpan.FromDays(group.TiempoMaximoPrestamoDias))
            .OrderBy(group => group.TiempoMaximoPrestamoDias)
            .FirstOrDefault();

        return exceeded == null
            ? null
            : $"El grupo '{exceeded.Nombre}' permite préstamos de hasta {exceeded.TiempoMaximoPrestamoDias} día(s)";
    }

    private async Task RollbackCreate(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction,
        int prestamoId
    )
    {
        if (transaction != null)
        {
            await transaction.RollbackAsync();
            return;
        }

        var details = await DbContext
            .DetallesPrestamos.Where(detail => detail.IdPrestamo == prestamoId)
            .ToListAsync();
        DbContext.DetallesPrestamos.RemoveRange(details);

        var loan = await DbContext.Prestamos.FirstOrDefaultAsync(item => item.Id == prestamoId);

        if (loan != null)
            DbContext.Prestamos.Remove(loan);

        await DbContext.SaveChangesAsync();
    }

    public async Task UpdateTracked(PrestamoEntity entity)
    {
        DbContext.Prestamos.Update(entity);
        await DbContext.SaveChangesAsync();
    }

    public async Task<List<(int CodigoImt, string? NombreGrupoEquipo, string EstadoEquipo)>> ApplyEstadoEquipoRetorno(
        int prestamoId,
        Dictionary<int, EstadoEquipo> statesByCode
    )
    {
        var details = await DbContext
            .DetallesPrestamos.Where(detail =>
                detail.IdPrestamo == prestamoId && detail.IdEquipo != null
            )
            .ToListAsync();

        var equipmentIds = details.Select(detail => detail.IdEquipo!.Value).ToHashSet();

        var equipment = await DbContext.Equipos.Where(item => equipmentIds.Contains(item.Id)).ToListAsync();

        var groupIds = equipment.Select(item => item.IdGrupoEquipo).ToHashSet();
        var groups = await DbContext
            .GruposEquipos.Where(group => groupIds.Contains(group.Id))
            .ToDictionaryAsync(group => group.Id, group => group.Nombre);

        var detailsByEquipment = details.ToDictionary(detail => detail.IdEquipo!.Value);
        var result = new List<(int, string?, string)>();

        foreach (var item in equipment)
        {
            if (!statesByCode.TryGetValue(item.CodigoImt, out var state))
                continue;

            item.EstadoEquipo = state;

            if (detailsByEquipment.TryGetValue(item.Id, out var detail))
                detail.EstadoEquipoRetorno = state;

            groups.TryGetValue(item.IdGrupoEquipo, out var name);
            result.Add((item.CodigoImt, name, ToPostgresEstadoEquipo(state)));
        }

        await DbContext.SaveChangesAsync();

        return result;
    }

    private static string ToPostgresEstadoEquipo(EstadoEquipo state) =>
        state switch
        {
            EstadoEquipo.ParcialmenteOperativo => "parcialmente_operativo",
            EstadoEquipo.Inoperativo => "inoperativo",
            _ => "operativo",
        };

    public async Task<bool> HasAvailableEquipo(
        int grupoEquipoId,
        DateTime startDate,
        DateTime endDate
    )
        => await HasAvailableEquipos(grupoEquipoId, 1, startDate, endDate);

    public async Task<bool> HasAvailableEquipos(
        int grupoEquipoId,
        int requiredQuantity,
        DateTime startDate,
        DateTime endDate
    )
    {
        if (requiredQuantity <= 0 || endDate <= startDate)
            return false;

        var availableQuantity = await DbContext
            .Equipos.Where(equipment =>
                equipment.IdGrupoEquipo == grupoEquipoId
                && !equipment.EstadoEliminado
                && equipment.EstadoEquipo == EstadoEquipo.Operativo
                && !DbContext
                    .DetallesPrestamos.Join(
                        DbContext.Prestamos,
                        detail => detail.IdPrestamo,
                        loan => loan.Id,
                        (detail, loan) => new { Detail = detail, Loan = loan }
                    )
                    .Any(activeLoan =>
                        activeLoan.Detail.IdEquipo == equipment.Id
                        && (
                            activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Aprobado
                            || activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Pendiente
                            || activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Activo
                            || activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Atrasado
                        )
                        && activeLoan.Loan.FechaPrestamoEsperada < endDate
                        && activeLoan.Loan.FechaDevolucionEsperada > startDate
                    )
                && !DbContext
                    .DetallesMantenimientos.Join(
                        DbContext.Mantenimientos,
                        detail => detail.IdMantenimiento,
                        maintenance => maintenance.Id,
                        (detail, maintenance) => new { Detail = detail, Maintenance = maintenance }
                    )
                    .Any(activeMaintenance =>
                        activeMaintenance.Detail.IdEquipo == equipment.Id
                        && activeMaintenance.Maintenance.FechaMantenimiento < endDate
                        && activeMaintenance.Maintenance.FechaFinalMantenimiento > startDate
                    )
            )
            .CountAsync();

        var unassignedReservations = await DbContext
            .DetallesPrestamos.Join(
                DbContext.Prestamos,
                detail => detail.IdPrestamo,
                loan => loan.Id,
                (detail, loan) => new { Detail = detail, Loan = loan }
            )
            .CountAsync(reservation =>
                reservation.Detail.IdGrupoEquipo == grupoEquipoId
                && reservation.Detail.IdEquipo == null
                && reservation.Loan.EstadoPrestamo == EstadoPrestamo.Pendiente
                && reservation.Loan.FechaPrestamoEsperada < endDate
                && reservation.Loan.FechaDevolucionEsperada > startDate
            );

        return availableQuantity - unassignedReservations >= requiredQuantity;
    }

    public async Task<bool> AssignEquiposOnApproval(int prestamoId)
    {
        var loan = await DbContext.Prestamos.FirstOrDefaultAsync(loan => loan.Id == prestamoId);

        if (loan == null)
            return false;

        var details = await DbContext
            .DetallesPrestamos.Where(detail =>
                detail.IdPrestamo == prestamoId && !detail.EstadoEliminado && detail.IdEquipo == null
            )
            .ToListAsync();

        if (details.Count == 0)
            return true;

        var loanedIds = await DbContext
            .DetallesPrestamos.Join(
                DbContext.Prestamos,
                detail => detail.IdPrestamo,
                loan => loan.Id,
                (detail, loan) => new { Detail = detail, Loan = loan }
            )
            .Where(activeLoan =>
                activeLoan.Detail.IdEquipo != null
                && activeLoan.Loan.Id != prestamoId
                && (
                    activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Pendiente
                    || activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Aprobado
                    || activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Activo
                    || activeLoan.Loan.EstadoPrestamo == EstadoPrestamo.Atrasado
                )
                && activeLoan.Loan.FechaPrestamoEsperada < loan.FechaDevolucionEsperada
                && activeLoan.Loan.FechaDevolucionEsperada > loan.FechaPrestamoEsperada
            )
            .Select(activeLoan => activeLoan.Detail.IdEquipo!.Value)
            .ToListAsync();

        var requiredGroups = details.Select(detail => detail.IdGrupoEquipo).ToHashSet();

        var candidatesByGroup = (
            await DbContext
                .Equipos.Where(equipment =>
                    requiredGroups.Contains(equipment.IdGrupoEquipo)
                    && !equipment.EstadoEliminado
                    && equipment.EstadoEquipo == EstadoEquipo.Operativo
                    && !loanedIds.Contains(equipment.Id)
                    && !DbContext
                        .DetallesMantenimientos.Join(
                            DbContext.Mantenimientos,
                            detail => detail.IdMantenimiento,
                            maintenance => maintenance.Id,
                            (detail, maintenance) => new { Detail = detail, Maintenance = maintenance }
                        )
                        .Any(activeMaintenance =>
                            activeMaintenance.Detail.IdEquipo == equipment.Id
                            && activeMaintenance.Maintenance.FechaMantenimiento
                                < loan.FechaDevolucionEsperada
                            && activeMaintenance.Maintenance.FechaFinalMantenimiento
                                > loan.FechaPrestamoEsperada
                        )
                )
                .OrderBy(equipment => equipment.Id)
                .Select(equipment => new { equipment.Id, equipment.IdGrupoEquipo })
                .ToListAsync()
        )
            .GroupBy(equipment => equipment.IdGrupoEquipo)
            .ToDictionary(
                group => group.Key,
                group => new Queue<int>(group.Select(equipment => equipment.Id))
            );

        foreach (var detail in details)
        {
            if (
                !candidatesByGroup.TryGetValue(detail.IdGrupoEquipo, out var available)
                || available.Count == 0
            )
                return false;

            detail.IdEquipo = available.Dequeue();
        }

        await DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<List<ContractEquipmentData>> GetContractEquipment(int prestamoId) =>
        await (
            from detail in DbContext.DetallesPrestamos.AsNoTracking()
            join equipment in DbContext.Equipos.AsNoTracking()
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
        ).ToListAsync();

    public async Task<string?> GetGrupoEquipoNombre(int grupoEquipoId) =>
        await DbContext
            .GruposEquipos.AsNoTracking()
            .Where(group => group.Id == grupoEquipoId && !group.EstadoEliminado)
            .Select(group => group.Nombre)
            .FirstOrDefaultAsync();

    public async Task<bool> HasAtrasadoPrestamo(string carnet) =>
        await DbContext.Prestamos.AnyAsync(loan =>
            loan.Carnet == carnet
            && loan.EstadoPrestamo == EstadoPrestamo.Atrasado
            && !loan.EstadoEliminado
        );

    public async Task<bool> IsUserBlocked(string carnet) =>
        await DbContext.Usuarios.AnyAsync(user => user.Carnet == carnet && user.Bloqueado);

    public async Task<string?> GetBlockReason(string carnet) =>
        await DbContext
            .Usuarios.Where(user => user.Carnet == carnet)
            .Select(user => user.MotivoBloqueo)
            .FirstOrDefaultAsync();

    public async Task<List<PrestamoDto>> GetOverdueLoans(DateTime now) =>
        await ToLoanDtos(
            DbContext.Prestamos.Where(loan =>
                loan.EstadoPrestamo == EstadoPrestamo.Activo
                && loan.FechaDevolucionEsperada < now
                && !loan.EstadoEliminado
            )
        );

    public async Task<List<PrestamoDto>> GetExpiredPendingLoans(DateTime now) =>
        await ToLoanDtos(
            DbContext.Prestamos.Where(loan =>
                (
                    loan.EstadoPrestamo == EstadoPrestamo.Pendiente
                    || loan.EstadoPrestamo == EstadoPrestamo.Aprobado
                )
                && loan.FechaPrestamoEsperada < now
                && !loan.EstadoEliminado
            )
        );

    public async Task<List<PrestamoDto>> GetLoansDueForReminder(DateTime now, DateTime reminderDeadline) =>
        await ToLoanDtos(
            DbContext.Prestamos.Where(loan =>
                loan.EstadoPrestamo == EstadoPrestamo.Activo
                && loan.FechaDevolucionEsperada > now
                && loan.FechaDevolucionEsperada <= reminderDeadline
                && !loan.RecordatorioEnviado
                && !loan.EstadoEliminado
            )
        );

    public async Task MarkAsOverdue(IReadOnlyCollection<int> ids) =>
        await UpdateStatus(ids, EstadoPrestamo.Atrasado);

    public async Task MarkAsRejected(IReadOnlyCollection<int> ids) =>
        await UpdateStatus(ids, EstadoPrestamo.Rechazado);

    public async Task MarkReminderSent(IReadOnlyCollection<int> ids)
    {
        if (ids.Count == 0)
            return;

        await DbContext
            .Prestamos.Where(loan => ids.Contains(loan.Id))
            .ExecuteUpdateAsync(update => update.SetProperty(loan => loan.RecordatorioEnviado, true));
    }

    private async Task UpdateStatus(IReadOnlyCollection<int> ids, EstadoPrestamo estado)
    {
        if (ids.Count == 0)
            return;

        await DbContext
            .Prestamos.Where(loan => ids.Contains(loan.Id))
            .ExecuteUpdateAsync(update => update.SetProperty(loan => loan.EstadoPrestamo, estado));
    }

    private static async Task<List<PrestamoDto>> ToLoanDtos(IQueryable<PrestamoEntity> query) =>
        await query
            .Select(loan => new PrestamoDto
            {
                Id = loan.Id,
                CarnetUsuario = loan.Carnet ?? string.Empty,
            })
            .ToListAsync();

    private async Task<List<PrestamoDto>> GetPrestamoList(IQueryable<PrestamoEntity> source)
    {
        var rows = await (
            from prestamo in source.OrderByDescending(p => p.FechaSolicitud)
            join usuario in DbContext.Usuarios.AsNoTracking().IgnoreQueryFilters()
                on prestamo.Carnet equals usuario.Carnet
                into usuarioJoin
            from usuario in usuarioJoin.DefaultIfEmpty()
            join detalle in DbContext
                .DetallesPrestamos.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(d => !d.EstadoEliminado)
                on prestamo.Id equals detalle.IdPrestamo
                into detalleJoin
            from detalle in detalleJoin.DefaultIfEmpty()
            join equipo in DbContext.Equipos.AsNoTracking().IgnoreQueryFilters()
                on detalle.IdEquipo equals equipo.Id
                into equipoJoin
            from equipo in equipoJoin.DefaultIfEmpty()
            join grupoReserva in DbContext.GruposEquipos.AsNoTracking().IgnoreQueryFilters()
                on detalle.IdGrupoEquipo equals grupoReserva.Id
                into grupoReservaJoin
            from grupoReserva in grupoReservaJoin.DefaultIfEmpty()
            join gavetero in DbContext.Gaveteros.AsNoTracking().IgnoreQueryFilters()
                on equipo.IdGavetero equals gavetero.Id
                into gaveteroJoin
            from gavetero in gaveteroJoin.DefaultIfEmpty()
            join mueble in DbContext.Muebles.AsNoTracking().IgnoreQueryFilters()
                on gavetero.IdMueble equals mueble.Id
                into muebleJoin
            from mueble in muebleJoin.DefaultIfEmpty()
            select new
            {
                PrestamoId = prestamo.Id,
                prestamo.Carnet,
                UsuarioNombre = usuario != null ? usuario.Nombre : null,
                UsuarioApellido = usuario != null ? usuario.ApellidoPaterno : null,
                UsuarioTelefono = usuario != null ? usuario.Telefono : null,
                prestamo.EstadoPrestamo,
                prestamo.FechaSolicitud,
                prestamo.FechaPrestamoEsperada,
                prestamo.FechaPrestamo,
                prestamo.FechaDevolucionEsperada,
                prestamo.FechaDevolucion,
                prestamo.Observacion,
                prestamo.IdContrato,
                NombreGrupoEquipo = grupoReserva != null ? grupoReserva.Nombre : null,
                CodigoImt = equipo != null ? (int?)equipo.CodigoImt : null,
                UbicacionEquipo = equipo != null ? equipo.Ubicacion : null,
                NombreGavetero = gavetero != null ? gavetero.Nombre : null,
                NombreMueble = mueble != null ? mueble.Nombre : null,
                UbicacionMueble = mueble != null ? mueble.Ubicacion : null,
            }
        ).ToListAsync();

        return rows.Select(r => new PrestamoDto
        {
            Id = r.PrestamoId,
            CarnetUsuario = r.Carnet,
            NombreUsuario = r.UsuarioNombre,
            ApellidoPaternoUsuario = r.UsuarioApellido,
            TelefonoUsuario = r.UsuarioTelefono,
            EstadoPrestamo = PrestamoState.ToText(r.EstadoPrestamo),
            FechaSolicitud = r.FechaSolicitud,
            FechaPrestamoEsperada = r.FechaPrestamoEsperada,
            FechaPrestamo = r.FechaPrestamo,
            FechaDevolucionEsperada = r.FechaDevolucionEsperada,
            FechaDevolucion = r.FechaDevolucion,
            Observacion = r.Observacion,
            IdContrato = r.IdContrato,
            NombreGrupoEquipo = r.NombreGrupoEquipo,
            CodigoImt = r.CodigoImt?.ToString(CultureInfo.InvariantCulture),
            UbicacionEquipo = r.UbicacionEquipo,
            NombreGavetero = r.NombreGavetero,
            NombreMueble = r.NombreMueble,
            UbicacionMueble = r.UbicacionMueble,
        })
            .ToList();
    }
}
