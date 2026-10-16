namespace NFHotel.Infrastructure.Config;

/// <summary>
/// Indstillinger for feltkryptering (B-09).
/// </summary>
/// <remarks>
/// Nøglen kommer fra <c>dotnet user-secrets</c> i udvikling og fra miljøvariabler eller
/// pipeline-secrets i drift (D-07) — aldrig fra appsettings.json i versionsstyring.
/// <para>
/// <b>Appen nægter at starte uden nøgle.</b> Valideringen sker i
/// <see cref="DependencyInjection.AddInfrastructure"/>, altså allerede når containeren
/// bygges. Alternativet — at fejle først ved første gæsteopdatering — ville betyde at
/// pasnumre kunne nå at blive gemt i klartekst uden at nogen opdagede det.
/// </para>
/// </remarks>
public sealed class EncryptionOptions
{
    /// <summary>Konfigurationssektionen indstillingerne læses fra.</summary>
    public const string SectionName = "Encryption";

    /// <summary>Nøglens navn i konfigurationen, uden sektionspræfiks.</summary>
    public const string PassportKeyName = "PassportKey";

    /// <summary>
    /// AES-nøglen, base64-kodet. Skal afkode til 16, 24 eller 32 bytes (AES-128/192/256).
    /// </summary>
    public required string PassportKey { get; init; }
}