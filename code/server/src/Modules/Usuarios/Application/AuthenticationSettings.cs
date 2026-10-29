namespace IMT_Reservas.Server.Application.Abstraction;

public sealed class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    public ExternalIdentityProvider Provider { get; set; } = ExternalIdentityProvider.Google;
    public string FrontendUrl { get; set; } = string.Empty;
    public string InstitutionalDomain { get; set; } = string.Empty;

    public string FrontendBaseUrl => FrontendUrl.TrimEnd('/');
}
