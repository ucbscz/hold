namespace IMT_Reservas.Server.Presentation.Security;

public static class ApiPaths
{
    public const string Api = "/api";
    public const string Auth = "/api/auth";
    public const string Health = "/api/health";
    private const string CallbackSegment = "callback";

    public static string ExternalCallback(string provider) =>
        $"{Auth}/{provider.ToLowerInvariant()}/{CallbackSegment}";
}
