using Ardalis.Result;
using UCB_Hold.Server.Modules.Prestamos.Application.State;
using UCB_Hold.Server.Modules.Prestamos.Domain;
using UCB_Hold.Server.Modules.Prestamos.Infrastructure;

namespace UCB_Hold.Server.Modules.Prestamos.Application;

public sealed class PrestamoConsultaService
{
    private readonly PrestamoReadRepository _queries;

    public PrestamoConsultaService(PrestamoReadRepository queries) => _queries = queries;

    public Task<Result<PrestamoDto>> GetAuthorized(
        int id,
        string carnet,
        bool isAdmin,
        CancellationToken cancellationToken = default
    ) => _queries.GetAuthorized(id, carnet, isAdmin, cancellationToken);

    public async Task<Result<List<PrestamoDto>>> GetHistory(
        string carnetUsuario,
        string estadoPrestamo,
        int page = 1,
        int pageSize = PrestamoReadRepository.MaxPageSize,
        bool? guardado = null,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(carnetUsuario))
            return Result<List<PrestamoDto>>.Error("Carnet requerido");

        return await GetFiltered(
            carnetUsuario,
            estadoPrestamo,
            page,
            pageSize,
            guardado,
            cancellationToken
        );
    }

    public async Task<Result<List<PrestamoDto>>> GetFiltered(
        string? carnetUsuario,
        string estadoPrestamo,
        int page = 1,
        int pageSize = PrestamoReadRepository.MaxPageSize,
        bool? guardado = null,
        CancellationToken cancellationToken = default
    )
    {
        EstadoPrestamo? estado = null;

        if (!string.IsNullOrEmpty(estadoPrestamo) && estadoPrestamo != PrestamoState.TodosFiltro)
        {
            estado = PrestamoState.Parse(estadoPrestamo);

            if (!estado.HasValue)
                return Result<List<PrestamoDto>>.Error("Estado préstamo no válido");
        }

        return await _queries.GetHistoryWithDetails(
            carnetUsuario,
            estado,
            page,
            pageSize,
            guardado,
            cancellationToken
        );
    }
}
