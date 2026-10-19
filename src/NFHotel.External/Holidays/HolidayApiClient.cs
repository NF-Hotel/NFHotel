using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NFHotel.Application.Dto.Holidays;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.External.Config;

namespace NFHotel.External.Holidays;

/// <summary>
/// The HTTP implementation of <see cref="IHolidayCalendarClient"/> against the analysis service's
/// <c>/api/v1/holidays</c>.
/// </summary>
/// <remarks>
/// The contract of the service (README, "Endpoints"):
/// <list type="bullet">
/// <item><c>GET /api/v1/holidays?year=</c> — the exported calendar.</item>
/// <item><c>POST /api/v1/holidays/update?first_year=&amp;last_year=</c> — generate it from scratch.</item>
/// </list>
/// Both require the header <c>X-API-Key</c>. The client translates all expected errors to
/// <see cref="Result"/> codes (A-09) and logs them; the API key and the response content are never logged.
/// </remarks>
public sealed class HolidayApiClient : IHolidayCalendarClient
{
    private const string HolidaysPath = "api/v1/holidays";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // The service delivers snake_case (first_year, start_date), and dates like "2024-04-13".
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly HttpClient _httpClient;
    private readonly HolidayApiOptions _options;
    private readonly ILogger<HolidayApiClient> _logger;

    /// <summary>Creates the client. <paramref name="httpClient"/> is configured by <c>AddHolidayApi</c>.</summary>
    /// <param name="httpClient">The client with address, timeout and key header.</param>
    /// <param name="options">The settings, used to decide whether a call can be attempted.</param>
    /// <param name="logger">Logger for expected errors.</param>
    public HolidayApiClient(HttpClient httpClient, HolidayApiOptions options, ILogger<HolidayApiClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<Result<HolidayCalendarDto>> GetAsync(int? year = null, CancellationToken cancellationToken = default)
    {
        var url = year is { } y
            ? $"{HolidaysPath}?year={y.ToString(CultureInfo.InvariantCulture)}"
            : HolidaysPath;

        return SendAsync(HttpMethod.Get, url, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Result<HolidayCalendarDto>> RefreshAsync(
        int? firstYear = null,
        int? lastYear = null,
        CancellationToken cancellationToken = default)
    {
        if (firstYear is { } first && lastYear is { } last && first > last)
        {
            return Task.FromResult(Result<HolidayCalendarDto>.Failure(ErrorCodes.Holidays.InvalidRequest));
        }

        var query = new List<string>(2);

        if (firstYear is { } f)
        {
            query.Add($"first_year={f.ToString(CultureInfo.InvariantCulture)}");
        }

        if (lastYear is { } l)
        {
            query.Add($"last_year={l.ToString(CultureInfo.InvariantCulture)}");
        }

        var url = query.Count == 0
            ? $"{HolidaysPath}/update"
            : $"{HolidaysPath}/update?{string.Join('&', query)}";

        return SendAsync(HttpMethod.Post, url, cancellationToken);
    }

    /// <summary>Sends one call and translates all expected outcomes to a <see cref="Result{TValue}"/>.</summary>
    private async Task<Result<HolidayCalendarDto>> SendAsync(
        HttpMethod method,
        string relativeUrl,
        CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning(
                "Holiday API is not configured: set {Section}:{ApiKey} (and {Section}:{BaseUrl} if it is not {Default}).",
                HolidayApiOptions.SectionName,
                HolidayApiOptions.ApiKeyName,
                HolidayApiOptions.SectionName,
                HolidayApiOptions.BaseUrlName,
                HolidayApiOptions.DefaultBaseUrl);

            return Result<HolidayCalendarDto>.Failure(ErrorCodes.Holidays.NotConfigured);
        }

        try
        {
            using var request = new HttpRequestMessage(method, relativeUrl);
            using var response = await _httpClient
                .SendAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Fail(method, response.StatusCode);
            }

            var wire = await response.Content
                .ReadFromJsonAsync<HolidayCalendarWire>(JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            return wire?.Holidays is null || wire.Country is null || wire.Source is null
                ? Unexpected(method, "the response body was empty or incomplete")
                : Result<HolidayCalendarDto>.Success(wire.ToDto());
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout: the caller has not asked for cancellation, so it is the service that is too slow.
            _logger.LogWarning("Holiday API {Method} timed out after {Seconds} s.", method, _options.TimeoutSeconds);

            return Result<HolidayCalendarDto>.Failure(ErrorCodes.Holidays.Unavailable);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Holiday API {Method} could not be reached.", method);

            return Result<HolidayCalendarDto>.Failure(ErrorCodes.Holidays.Unavailable);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Holiday API {Method} returned unreadable JSON.", method);

            return Result<HolidayCalendarDto>.Failure(ErrorCodes.Holidays.UnexpectedResponse);
        }
    }

    /// <summary>Translates an HTTP error status to an error code.</summary>
    private Result<HolidayCalendarDto> Fail(HttpMethod method, HttpStatusCode status)
    {
        var code = status switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ErrorCodes.Holidays.Unauthorized,
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ErrorCodes.Holidays.InvalidRequest,
            >= HttpStatusCode.InternalServerError => ErrorCodes.Holidays.Unavailable,
            _ => ErrorCodes.Holidays.UnexpectedResponse,
        };

        _logger.LogWarning("Holiday API {Method} answered HTTP {Status} ({Code}).", method, (int)status, code);

        return Result<HolidayCalendarDto>.Failure(code);
    }

    private Result<HolidayCalendarDto> Unexpected(HttpMethod method, string reason)
    {
        _logger.LogWarning("Holiday API {Method}: {Reason}.", method, reason);

        return Result<HolidayCalendarDto>.Failure(ErrorCodes.Holidays.UnexpectedResponse);
    }

    // ------------------------------------------------------------------
    // The JSON shape of the service (HolidayDocument in domain/schemas.py). Kept private, so
    // Application does not inherit the field names of the service.
    // ------------------------------------------------------------------

    private sealed record HolidayCalendarWire(
        string? Country,
        string? Source,
        int FirstYear,
        int LastYear,
        List<HolidayPeriodWire>? Holidays)
    {
        public HolidayCalendarDto ToDto() => new(
            Country!,
            Source!,
            FirstYear,
            LastYear,
            Holidays!.ConvertAll(h => new HolidayPeriodDto(h.Name, h.StartDate, h.EndDate)));
    }

    private sealed record HolidayPeriodWire(string Name, DateOnly StartDate, DateOnly EndDate);
}
