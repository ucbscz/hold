namespace UCB_Hold.Server.Shared.Configuration;

public sealed class BrandingSettings
{
    public const string SectionName = "Branding";

    public string ProductName { get; set; } = string.Empty;
    public string InternalCodeLabel { get; set; } = string.Empty;
    public string OrganizationCodeLabel { get; set; } = string.Empty;
}
