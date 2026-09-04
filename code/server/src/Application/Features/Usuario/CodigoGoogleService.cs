using IMT_Reservas.Server.Core.Entities;
using IMT_Reservas.Server.Infrastructure.Repositories.Implementations;

namespace IMT_Reservas.Server.Application.Features.Usuario;

public sealed class CodigoGoogleService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private readonly CodigoAutenticacionRepository _repository;

    public CodigoGoogleService(CodigoAutenticacionRepository repository) => _repository = repository;

    public async Task<string> Create(
        string type,
        string email,
        string googleId,
        string name,
        string paternalSurname,
        string maternalSurname,
        CancellationToken cancellationToken
    )
    {
        var token = AuthTokenGenerator.Create();
        await _repository.Create(
            new CodigoAutenticacion
            {
                Hash = AuthTokenGenerator.Hash(token),
                Tipo = type,
                Email = email,
                GoogleId = googleId,
                Nombre = name,
                ApellidoPaterno = paternalSurname,
                ApellidoMaterno = maternalSurname,
                Expira = DateTime.UtcNow.Add(Lifetime),
            },
            cancellationToken
        );
        return token;
    }

    public Task<CodigoAutenticacion?> Get(string token, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(token) || token.Length > 256
            ? Task.FromResult<CodigoAutenticacion?>(null)
            : _repository.GetActive(AuthTokenGenerator.Hash(token), cancellationToken);

    public Task<bool> Consume(string token, CancellationToken cancellationToken) =>
        _repository.Consume(AuthTokenGenerator.Hash(token), cancellationToken);
}
