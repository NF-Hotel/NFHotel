namespace NFHotel.External.Config;

/// <summary>
/// Settings for the connection to the analysis service (NF Hotel Analytics API).
/// </summary>
/// <remarks>
/// <b>The key does not belong in a file in the repo.</b> Set it with
/// <c>dotnet user-secrets set "HolidayApi:ApiKey" "…"</c> in development, and from a secret
/// store or the environment variable <c>HolidayApi__ApiKey</c> in operation.
/// <para>
/// Missing settings do not stop the start-up (unlike the encryption key):
/// the analysis service is an optional source, and the app must be able to run without it. Instead
/// the client returns <c>holidays.not_configured</c> on the first call.
/// </para>
/// </remarks>
public sealed class HolidayApiOptions
{
    /// <summary>The configuration section the settings are read from.</summary>
    public const string SectionName = "HolidayApi";

    /// <summary>The name of the key: the address of the service.</summary>
    public const string BaseUrlName = "BaseUrl";

    /// <summary>The name of the key: the API key.</summary>
    public const string ApiKeyName = "ApiKey";

    /// <summary>The name of the key: the timeout in seconds.</summary>
    public const string TimeoutSecondsName = "TimeoutSeconds";

    /// <summary>The address the service runs on, if the configuration says nothing else.</summary>
    public const string DefaultBaseUrl = "http://127.0.0.1:8000";

    /// <summary>The timeout if the configuration says nothing else. The calendar is a small file, so 30 seconds is plenty.</summary>
    public const int DefaultTimeoutSeconds = 30;

    /// <summary>The header the service expects the key in.</summary>
    public const string ApiKeyHeaderName = "X-API-Key";

    /// <summary>
    /// The root address of the service, for example <c>http://127.0.0.1:8000</c>. Without <c>/api/v1</c> — that
    /// path is part of the client.
    /// </summary>
    public string BaseUrl { get; init; } = DefaultBaseUrl;

    /// <summary>The key sent in <c>X-API-Key</c>. Empty means "not set".</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Timeout for ét kald, i sekunder.</summary>
    public int TimeoutSeconds { get; init; } = DefaultTimeoutSeconds;

    /// <summary>
    /// True if the address is a valid http(s) address and the key is set, so a call can
    /// be attempted.
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ApiKey) && TryGetBaseUri(out _);

    /// <summary>
    /// Tries to read <see cref="BaseUrl"/> as an absolute http(s) address with a trailing
    /// slash, so relative paths are placed <i>under</i> it rather than replacing its last segment.
    /// </summary>
    /// <param name="baseUri">The address, if it is valid.</param>
    /// <returns>True if the address can be used.</returns>
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

        // ! is justified: the out parameter must be assigned, and the caller must not read it when
        // the method returns false.
        baseUri = null!;
        return false;
    }
}