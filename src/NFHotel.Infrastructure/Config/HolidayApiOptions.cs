namespace NFHotel.Infrastructure.Config;

/// <summary>
/// Indstillinger for forbindelsen til analysetjenesten (NF Hotel Analytics API).
/// </summary>
/// <remarks>
/// <b>Nøglen hører ikke hjemme i en fil i repoet.</b> Sæt den med
/// <c>dotnet user-secrets set "HolidayApi:ApiKey" "…"</c> i udvikling, og fra en secret
/// store eller miljøvariablen <c>HolidayApi__ApiKey</c> i drift.
/// <para>
/// Manglende indstillinger stopper ikke opstarten (i modsætning til krypteringsnøglen):
/// analysetjenesten er en valgfri kilde, og appen skal kunne køre uden den. I stedet
/// returnerer klienten <c>holidays.not_configured</c> ved det første kald.
/// </para>
/// </remarks>
public sealed class HolidayApiOptions
{
    /// <summary>Konfigurationssektionen indstillingerne læses fra.</summary>
    public const string SectionName = "HolidayApi";

    /// <summary>Nøglens navn: tjenestens adresse.</summary>
    public const string BaseUrlName = "BaseUrl";

    /// <summary>Nøglens navn: API-nøglen.</summary>
    public const string ApiKeyName = "ApiKey";

    /// <summary>Nøglens navn: timeout i sekunder.</summary>
    public const string TimeoutSecondsName = "TimeoutSeconds";

    /// <summary>Adressen tjenesten kører på, hvis konfigurationen ikke siger andet.</summary>
    public const string DefaultBaseUrl = "http://127.0.0.1:8000";

    /// <summary>Timeout hvis konfigurationen ikke siger andet. Kalenderen er en lille fil, så 30 sekunder er rigeligt.</summary>
    public const int DefaultTimeoutSeconds = 30;

    /// <summary>Headeren tjenesten forventer nøglen i.</summary>
    public const string ApiKeyHeaderName = "X-API-Key";

    /// <summary>
    /// Tjenestens rod-adresse, fx <c>http://127.0.0.1:8000</c>. Uden <c>/api/v1</c> — den
    /// sti er en del af klienten.
    /// </summary>
    public string BaseUrl { get; init; } = DefaultBaseUrl;

    /// <summary>Nøglen der sendes i <c>X-API-Key</c>. Tom betyder "ikke sat".</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Timeout for ét kald, i sekunder.</summary>
    public int TimeoutSeconds { get; init; } = DefaultTimeoutSeconds;

    /// <summary>
    /// Sand hvis adressen er en gyldig http(s)-adresse og nøglen er sat, så et kald kan
    /// forsøges.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) && TryGetBaseUri(out _);

    /// <summary>
    /// Forsøger at læse <see cref="BaseUrl"/> som en absolut http(s)-adresse med afsluttende
    /// skråstreg, så relative stier lægges <i>under</i> den frem for at erstatte dens sidste led.
    /// </summary>
    /// <param name="baseUri">Adressen, hvis den er gyldig.</param>
    /// <returns>Sand hvis adressen kan bruges.</returns>
    public bool TryGetBaseUri(out Uri baseUri)
    {
        var text = BaseUrl.Trim();

        if (!text.EndsWith('/'))
        {
            text += "/";
        }

        if (Uri.TryCreate(text, UriKind.Absolute, out var parsed)
            && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
        {
            baseUri = parsed;
            return true;
        }

        // ! er begrundet: out-parameteren skal tildeles, og kalderen må ikke læse den når
        // metoden returnerer false.
        baseUri = null!;
        return false;
    }
}