namespace IMT_Reservas.Server.Shared.Security;

public sealed class DataProtectionSettings
{
    public const string SectionName = "DataProtection";

    public string ApplicationName { get; set; } = string.Empty;
    public string PayloadMarker { get; set; } = string.Empty;
    public string TextMarker { get; set; } = string.Empty;
    public string KeysPath { get; set; } = string.Empty;
    public string CertificatePath { get; set; } = string.Empty;
    public string CertificatePasswordFile { get; set; } = string.Empty;

    public string Purpose => $"{ApplicationName}.SensitiveData.v1";
}
