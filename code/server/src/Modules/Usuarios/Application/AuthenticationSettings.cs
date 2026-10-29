namespace UCB_Hold.Server.Modules.Usuarios.Application;

public sealed class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    public ExternalIdentityProvider Provider { get; set; } = ExternalIdentityProvider.Google;
    public string FrontendUrl { get; set; } = string.Empty;
    public string InstitutionalDomain { get; set; } = string.Empty;

    public string FrontendBaseUrl => FrontendUrl.TrimEnd('/');
}
