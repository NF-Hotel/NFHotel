namespace NFHotel.Infrastructure.Config;

/// <summary>
/// Indstillinger for feltkryptering (B-09).
/// </summary>
/// <remarks>
/// The key comes from <c>dotnet user-secrets</c> in development and from environment variables or
/// pipeline secrets in operation (D-07) — never from appsettings.json under version control.
/// <para>
/// <b>The app refuses to start without a key.</b> The validation happens in
/// <see cref="DependencyInjection.AddInfrastructure"/>, that is, already when the container
/// is built. The alternative — failing only at the first guest update — would mean that
/// passport numbers could be saved in plain text without anyone noticing.
/// </para>
/// </remarks>
public sealed class EncryptionOptions
{
    /// <summary>The configuration section the settings are read from.</summary>
    public const string SectionName = "Encryption";

    /// <summary>The name of the key in the configuration, without section prefix.</summary>
    public const string PassportKeyName = "PassportKey";

    /// <summary>
    /// The AES key, base64-encoded. Must decode to 16, 24 or 32 bytes (AES-128/192/256).
    /// </summary>
    public required string PassportKey { get; init; }
}