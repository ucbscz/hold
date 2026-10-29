namespace IMT_Reservas.Server.Shared.Configuration;

public static class BrandingRegistration
{
    public static IServiceCollection AddBranding(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var section = configuration.GetSection(BrandingSettings.SectionName);
        var settings = section.Get<BrandingSettings>() ?? new BrandingSettings();
        Require(settings.ProductName, nameof(BrandingSettings.ProductName));
        Require(settings.InternalCodeLabel, nameof(BrandingSettings.InternalCodeLabel));
        Require(settings.OrganizationCodeLabel, nameof(BrandingSettings.OrganizationCodeLabel));

        return services.Configure<BrandingSettings>(section);
    }

    private static void Require(string value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"{BrandingSettings.SectionName}:{key} is required."
            );
    }
}
