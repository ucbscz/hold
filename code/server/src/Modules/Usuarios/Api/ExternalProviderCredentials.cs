namespace IMT_Reservas.Server.Modules.Usuarios.Api;

public sealed class ExternalProviderCredentials
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    public bool IsPartial =>
        string.IsNullOrWhiteSpace(ClientId) != string.IsNullOrWhiteSpace(ClientSecret);
}
