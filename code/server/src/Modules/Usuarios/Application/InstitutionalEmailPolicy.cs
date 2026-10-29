using Microsoft.Extensions.Options;

namespace UCB_Hold.Server.Modules.Usuarios.Application;

public sealed class InstitutionalEmailPolicy
{
    private const char DomainSeparator = '@';

    public InstitutionalEmailPolicy(IOptions<AuthenticationSettings> settings)
    {
        Suffix = DomainSeparator + settings.Value.InstitutionalDomain.Trim().TrimStart(DomainSeparator);
    }

    public string Suffix { get; }

    public bool Accepts(string? email) =>
        !string.IsNullOrWhiteSpace(email)
        && email.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase);
}
