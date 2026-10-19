namespace NFHotel.External.Config;

/// <summary>
/// The settings for the data analysis service (repository <c>notebook-data-analysis</c>) that runs the market analysis
/// (MIL-026). Missing values are not a start-up error: the client answers
/// <c>market_analysis_runner.not_configured</c> so that the rest of the API keeps working.
/// </summary>
public sealed class MarketAnalysisServiceOptions
{
    /// <summary>The name of the configuration section.</summary>
    public const string SectionName = "MarketAnalysisService";

    /// <summary>The key of the address.</summary>
    public const string BaseUrlName = "BaseUrl";

    /// <summary>The key of the bearer token.</summary>
    public const string TokenName = "Token";

    /// <summary>The key of the timeout.</summary>
    public const string TimeoutSecondsName = "TimeoutSeconds";

    /// <summary>The seconds to wait for an answer; a start and a read return at once, so a short time is enough.</summary>
    public const int DefaultTimeoutSeconds = 30;

    /// <summary>
    /// The address of the data analysis service on the shared Docker network. There is no built-in address: it comes
    /// from the settings, and an empty address means the service is not configured.
    /// </summary>
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>The token that the data analysis service expects as a bearer token; a secret from the env file.</summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>The seconds to wait for an answer.</summary>
    public int TimeoutSeconds { get; init; } = DefaultTimeoutSeconds;

    /// <summary>True when there is a token and the address is an absolute http or https address.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Token) && TryGetBaseUri(out _);

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
