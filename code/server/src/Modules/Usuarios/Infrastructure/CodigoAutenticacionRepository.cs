using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Config;
using Microsoft.EntityFrameworkCore;

namespace IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

public sealed class CodigoAutenticacionRepository
{
    private readonly ApplicationDbContext _dbContext;

    public CodigoAutenticacionRepository(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task Create(CodigoAutenticacion code, CancellationToken cancellationToken)
    {
        if (_dbContext.Database.IsRelational())
            await _dbContext.CodigosAutenticacion
                .Where(item => item.Usado || item.Expira <= DateTime.UtcNow)
                .ExecuteDeleteAsync(cancellationToken);
        else
            _dbContext.CodigosAutenticacion.RemoveRange(
                _dbContext.CodigosAutenticacion.Where(item => item.Usado || item.Expira <= DateTime.UtcNow)
            );

        _dbContext.CodigosAutenticacion.Add(code);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<CodigoAutenticacion?> GetActive(string hash, CancellationToken cancellationToken) =>
        _dbContext.CodigosAutenticacion.AsNoTracking().FirstOrDefaultAsync(
            code => code.Hash == hash && !code.Usado && code.Expira > DateTime.UtcNow,
            cancellationToken
        );

    public async Task<bool> Consume(string hash, CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsRelational())
        {
            var code = await _dbContext.CodigosAutenticacion.FirstOrDefaultAsync(
                item => item.Hash == hash && !item.Usado && item.Expira > DateTime.UtcNow,
                cancellationToken
            );
            if (code == null)
                return false;
            code.Usado = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }

        return await _dbContext.CodigosAutenticacion
            .Where(code => code.Hash == hash && !code.Usado && code.Expira > DateTime.UtcNow)
            .ExecuteUpdateAsync(update => update.SetProperty(code => code.Usado, true), cancellationToken) == 1;
    }
}
