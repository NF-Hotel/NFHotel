using Microsoft.Extensions.Configuration;

using NFHotel.Api.Contracts;

namespace NFHotel.ApiClient;

/// <summary>Where the API is and how this application proves who it is (ADR-0004).</summary>
public sealed record ApiClientOptions
{
    /// <summary>The seconds a call may take when the setting is missing.</summary>
    public const int DefaultTimeoutSeconds = 30;

    /// <summary>The key of the timeout in the configuration section <see cref="ApiAuthentication.SectionName"/>.</summary>
    public const string TimeoutSecondsName = "TimeoutSeconds";

    /// <summary>The base address of the API, with a trailing slash.</summary>
    public required Uri BaseUri { get; init; }

    /// <summary>The bearer token.</summary>
    public required string Token { get; init; }

    /// <summary>The seconds a call may take.</summary>
    public int TimeoutSeconds { get; init; } = DefaultTimeoutSeconds;

    /// <summary>Reads the options and refuses a missing or invalid address or a missing token, so a setup mistake stops the start-up.</summary>
    /// <param name="configuration">The configuration with the section <c>Api</c>.</param>
    /// <returns>The options.</returns>
    public static ApiClientOptions Read(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(ApiAuthentication.SectionName);
        var baseUrl = section[ApiAuthentication.BaseUrlName]?.Trim();
        var token = section[ApiAuthentication.TokenName]?.Trim();

        if (string.IsNullOrEmpty(baseUrl)
            || !Uri.TryCreate(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/", UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"'{ApiAuthentication.SectionName}:{ApiAuthentication.BaseUrlName}' must be an absolute http or https address of the API.");
        }

        if (string.IsNullOrEmpty(token))
        {
            throw new InvalidOperationException($"'{ApiAuthentication.SectionName}:{ApiAuthentication.TokenName}' is missing.");
        }

        var timeout = int.TryParse(
            section[TimeoutSecondsName],
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture,
            out var seconds) && seconds > 0
            ? seconds
            : DefaultTimeoutSeconds;

        return new ApiClientOptions { BaseUri = baseUri, Token = token, TimeoutSeconds = timeout };
    }
}