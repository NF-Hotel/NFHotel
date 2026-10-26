using Microsoft.AspNetCore.DataProtection;

namespace NFHotel.Web.Security;

/// <summary>
/// Keeps the Data Protection keys of the web application in a folder, so the staff cookie and the antiforgery tokens
/// stay readable after the container is recreated (ADR-0031). Without the setting the framework default applies, which
/// is right for development and tests.
/// </summary>
public static class DataProtectionRegistration
{
    /// <summary>The setting that names the key folder; the image sets it to the folder of the volume.</summary>
    public const string KeysPathSetting = "DataProtection:KeysPath";

    /// <summary>The fixed application name, so a new container reads the keys of the one before it.</summary>
    public const string ApplicationName = "NFHotel.Web";

    /// <summary>Adds Data Protection with the fixed application name and, when the setting is given, the key folder.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration that holds <see cref="KeysPathSetting"/>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddPersistentDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var builder = services.AddDataProtection().SetApplicationName(ApplicationName);
        var keysPath = configuration[KeysPathSetting];
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            builder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        return services;
    }
}
