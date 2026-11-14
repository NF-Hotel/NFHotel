using System.Reflection;
using System.Security.Cryptography;

using NFHotel.Api.Contracts;
using NFHotel.Infrastructure;
using NFHotel.Infrastructure.Config;

namespace NFHotel.Api.Startup;

/// <summary>
/// The build of the OpenAPI document (MIL-077). When the API is built, the tool GetDocument.Insider starts it with no
/// server and no settings to read its endpoints; the settings that the start-up demands then get stand-ins that reach
/// nothing: random keys, and a database that is never opened.
/// </summary>
public static class OpenApiDocumentBuild
{
    /// <summary>The name of the assembly of the tool that writes the document at build time.</summary>
    public const string ToolName = "GetDocument.Insider";

    /// <summary>Gets a value indicating whether the API runs inside the build of its OpenAPI document.</summary>
    public static bool IsRunning { get; } =
        string.Equals(Assembly.GetEntryAssembly()?.GetName().Name, ToolName, StringComparison.Ordinal);

    /// <summary>The stand-ins for the settings that the start-up demands, new on each build.</summary>
    /// <returns>The settings by their configuration key.</returns>
    public static Dictionary<string, string?> StandInSettings() => new(StringComparer.Ordinal)
    {
        [$"{ApiAuthentication.SectionName}:{ApiAuthentication.TokenName}"] = RandomKey(48),
        [$"ConnectionStrings:{DependencyInjection.ConnectionStringName}"] = "Host=localhost;Database=never-connected",
        [$"{EncryptionOptions.SectionName}:{EncryptionOptions.PassportKeyName}"] = RandomKey(32),
        [$"{StaffSecurityOptions.SectionName}:Token:SigningKey"] = RandomKey(48),
    };

    private static string RandomKey(int byteCount) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteCount));
}
