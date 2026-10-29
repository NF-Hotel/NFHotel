using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NFHotel.Application.Ports;
using NFHotel.External.Benchmark;
using NFHotel.External.Config;
using NFHotel.External.Holidays;
using NFHotel.External.MarketAnalysis;
using NFHotel.External.Time;

namespace NFHotel.External;

/// <summary>
/// Registration of the adapters that are not the database: the hotel clock and the holiday calendar
/// client. The API uses the clock; the web application uses both (ADR-0004).
/// </summary>
public static class DependencyInjection
{
    /// <summary>Registers <see cref="IClock"/> in the time zone of the hotel (A-05).</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration, with the section <c>HotelTime</c>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddHotelClock(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddTime(services, ReadTimeOptions(configuration));
        return services;
    }

    /// <summary>Registers the typed <c>HttpClient</c> of the holiday calendar of the analysis service.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration, with the section <c>HolidayApi</c>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddHolidayApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddHolidayApi(services, ReadHolidayApiOptions(configuration));
        return services;
    }

    /// <summary>Registers the clock. Singleton: stateless apart from the immutable time zone.</summary>
    /// <summary>
    /// Registers the client of the benchmark service that runs the models (MIL-020). The address and the bearer token come
    /// from the section <c>BenchmarkService</c>; missing values are not a start-up error, the client answers
    /// <c>benchmark_runner.not_configured</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddBenchmarkService(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = ReadBenchmarkServiceOptions(configuration);
        services.AddSingleton(options);

        services.AddHttpClient<IBenchmarkServiceClient, BenchmarkServiceClient>(client =>
        {
            if (options.TryGetBaseUri(out var baseUri))
            {
                client.BaseAddress = baseUri;
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(options.Token))
            {
                client.DefaultRequestHeaders.Authorization = new("Bearer", options.Token);
            }
        });

        return services;
    }

    /// <summary>
    /// Registers the client of the market analysis service that runs the market analysis (MIL-026). The address, the API key
    /// and the time limits come from the section <c>MarketAnalysisService</c>; missing values are not a start-up error, the
    /// client answers <c>market_analysis_runner.not_configured</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddMarketAnalysisService(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(MarketAnalysisServiceOptions.SectionName);

        var options = new MarketAnalysisServiceOptions
        {
            BaseUrl = section[MarketAnalysisServiceOptions.BaseUrlName]?.Trim() ?? string.Empty,
            ApiKey = section[MarketAnalysisServiceOptions.ApiKeyName]?.Trim() ?? string.Empty,
            TimeoutSeconds = Math.Min(
                PositiveSeconds(section[MarketAnalysisServiceOptions.TimeoutSecondsName], MarketAnalysisServiceOptions.DefaultTimeoutSeconds),
                MarketAnalysisServiceOptions.MaxTimeoutSeconds),
            PollIntervalSeconds = PositiveSeconds(section[MarketAnalysisServiceOptions.PollIntervalSecondsName], MarketAnalysisServiceOptions.DefaultPollIntervalSeconds),
            RunDeadlineSeconds = PositiveSeconds(section[MarketAnalysisServiceOptions.RunDeadlineSecondsName], MarketAnalysisServiceOptions.DefaultRunDeadlineSeconds),
        };
        services.AddSingleton(options);

        services.AddHttpClient<IMarketAnalysisServiceClient, MarketAnalysisServiceClient>(client =>
        {
            if (options.TryGetBaseUri(out var baseUri))
            {
                client.BaseAddress = baseUri;
            }

            // Every call is short: the client follows the run by its id instead of keeping one request open.
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });

        return services;
    }

    private static int PositiveSeconds(string? text, int fallback) =>
        int.TryParse(text, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var seconds) && seconds > 0
            ? seconds
            : fallback;

    private static void AddTime(IServiceCollection services, HotelTimeOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IClock, SystemClock>();
    }

    /// <summary>
    /// Registers the client for the holiday calendar of the analysis service as a typed
    /// <see cref="HttpClient"/> (<see cref="IHttpClientFactory"/> owns the lifetime of the handler).
    /// </summary>
    private static void AddHolidayApi(IServiceCollection services, HolidayApiOptions options)
    {
        services.AddSingleton(options);

        services.AddHttpClient<IHolidayCalendarClient, HolidayApiClient>(client =>
        {
            // An invalid address or missing key is not a start-up error: the client answers
            // holidays.not_configured (see HolidayApiOptions). We therefore configure only what we can.
            if (options.TryGetBaseUri(out var baseUri))
            {
                client.BaseAddress = baseUri;
            }

            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add(HolidayApiOptions.ApiKeyHeaderName, options.ApiKey);
            }
        });
    }

    /// <summary>
    /// Reads the settings for the analysis service. Does not fail on missing values — see
    /// <see cref="HolidayApiOptions"/> — but falls back to the default address and timeout.
    /// </summary>
    private static HolidayApiOptions ReadHolidayApiOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(HolidayApiOptions.SectionName);
        var baseUrl = section[HolidayApiOptions.BaseUrlName];

        var timeout = int.TryParse(
            section[HolidayApiOptions.TimeoutSecondsName],
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture,
            out var seconds) && seconds > 0
            ? seconds
            : HolidayApiOptions.DefaultTimeoutSeconds;

        return new HolidayApiOptions
        {
            BaseUrl = string.IsNullOrWhiteSpace(baseUrl) ? HolidayApiOptions.DefaultBaseUrl : baseUrl,
            ApiKey = section[HolidayApiOptions.ApiKeyName]?.Trim() ?? string.Empty,
            TimeoutSeconds = timeout,
        };
    }

    private static BenchmarkServiceOptions ReadBenchmarkServiceOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(BenchmarkServiceOptions.SectionName);
        var baseUrl = section[BenchmarkServiceOptions.BaseUrlName];

        var timeout = int.TryParse(
            section[BenchmarkServiceOptions.TimeoutSecondsName],
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture,
            out var seconds) && seconds > 0
            ? seconds
            : BenchmarkServiceOptions.DefaultTimeoutSeconds;

        return new BenchmarkServiceOptions
        {
            BaseUrl = baseUrl?.Trim() ?? string.Empty,
            Token = section[BenchmarkServiceOptions.TokenName]?.Trim() ?? string.Empty,
            TimeoutSeconds = timeout,
        };
    }

    /// <summary>Reads the time settings and verifies that the time zone exists on the machine.</summary>
    private static HotelTimeOptions ReadTimeOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection(HotelTimeOptions.SectionName);
        var timeZoneId = section[HotelTimeOptions.TimeZoneIdName];

        var options = new HotelTimeOptions
        {
            TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId)
                ? HotelTimeOptions.DefaultTimeZoneId
                : timeZoneId
        };

        // Fail fast: an unknown time zone must not be discovered only when someone checks in (A-05).
        _ = new SystemClock(options);

        return options;
    }
}
