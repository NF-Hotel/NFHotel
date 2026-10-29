namespace NFHotel.External.Config;

/// <summary>
/// The settings for the market analysis service (repository <c>notebook-market-analysis</c>, run API of MIL-028) that runs the
/// market analysis (MIL-026). A missing address is not a start-up error: the client answers
/// <c>market_analysis_runner.not_configured</c> so that the rest of the API keeps working.
/// </summary>
public sealed class MarketAnalysisServiceOptions
{
    /// <summary>The name of the configuration section.</summary>
    public const string SectionName = "MarketAnalysisService";

    /// <summary>The key of the address.</summary>
    public const string BaseUrlName = "BaseUrl";

    /// <summary>The key of the API key.</summary>
    public const string ApiKeyName = "ApiKey";

    /// <summary>The key of the timeout of one HTTP call.</summary>
    public const string TimeoutSecondsName = "TimeoutSeconds";

    /// <summary>The key of the seconds between two asks for the state of a run.</summary>
    public const string PollIntervalSecondsName = "PollIntervalSeconds";

    /// <summary>The key of the seconds to follow one run before it is given up.</summary>
    public const string RunDeadlineSecondsName = "RunDeadlineSeconds";

    /// <summary>
    /// The seconds to wait for one HTTP call: a start, a state or a result. No call lasts while the models work (the run
    /// API answers a start at once and the client asks for the state), so a few seconds are enough.
    /// </summary>
    public const int DefaultTimeoutSeconds = 10;

    /// <summary>The longest timeout of one HTTP call that is accepted, so that an old setting of 90000 cannot keep a call open.</summary>
    public const int MaxTimeoutSeconds = 60;

    /// <summary>The seconds between two asks for the state of a run.</summary>
    public const int DefaultPollIntervalSeconds = 5;

    /// <summary>
    /// The seconds to follow one run before it is given up as unavailable. A run with AI insights lasts up to 12 minutes with
    /// the defaults of the service, and longer with a slow model on a machine without a GPU; the default is 60 minutes.
    /// </summary>
    public const int DefaultRunDeadlineSeconds = 3600;

    /// <summary>
    /// The address of the market analysis service on the shared Docker network. There is no built-in address: it comes from the
    /// settings, and an empty address means the service is not configured.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// The key sent as the header <c>X-API-Key</c> of every call. It is a secret: it is never logged. When it is empty the
    /// header is left out, and a service that needs a key answers 401.
    /// </summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>The seconds to wait for one HTTP call.</summary>
    public int TimeoutSeconds { get; init; } = DefaultTimeoutSeconds;

    /// <summary>The seconds between two asks for the state of a run.</summary>
    public int PollIntervalSeconds { get; init; } = DefaultPollIntervalSeconds;

    /// <summary>The seconds to follow one run before it is given up.</summary>
    public int RunDeadlineSeconds { get; init; } = DefaultRunDeadlineSeconds;

    /// <summary>True when the address is an absolute http or https address.</summary>
    public bool IsConfigured => TryGetBaseUri(out _);

    /// <summary>Reads the address as an absolute URI that ends in a slash, so that relative paths are added to it.</summary>
    /// <param name="baseUri">The address when the method returns true.</param>
    /// <returns>True when the address is a usable one.</returns>
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
