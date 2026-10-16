using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NFHotel.Application.Dto.Holidays;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Infrastructure.Config;

namespace NFHotel.Infrastructure.Holidays;

/// <summary>
/// HTTP-implementeringen af <see cref="IHolidayCalendarClient"/> mod analysetjenestens
/// <c>/api/v1/holidays</c>.
/// </summary>
/// <remarks>
/// Tjenestens kontrakt (README, "Endpoints"):
/// <list type="bullet">
/// <item><c>GET /api/v1/holidays?year=</c> — den eksporterede kalender.</item>
/// <item><c>POST /api/v1/holidays/update?first_year=&amp;last_year=</c> — generér den forfra.</item>
/// </list>
/// Begge kræver headeren <c>X-API-Key</c>. Klienten oversætter alle forventede fejl til
/// <see cref="Result"/>-koder (A-09) og logger dem; API-nøglen og svarindholdet logges aldrig.
/// </remarks>
public sealed class HolidayApiClient : IHolidayCalendarClient
{
    private const string HolidaysPath = "api/v1/holidays";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // Tjenesten leverer snake_case (first_year, start_date), og datoer som "2024-04-13".
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly HttpClient _httpClient;
    private readonly HolidayApiOptions _options;
    private readonly ILogger<HolidayApiClient> _logger;

    /// <summary>Opretter klienten. <paramref name="httpClient"/> er konfigureret af <c>AddHolidayApi</c>.</summary>
    /// <param name="httpClient">Klienten med adresse, timeout og nøgle-header.</param>
    /// <param name="options">Indstillingerne, bruges til at afgøre om et kald kan forsøges.</param>
    /// <param name="logger">Logger til forventede fejl.</param>
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

    /// <summary>Sender ét kald og oversætter alle forventede udfald til et <see cref="Result{TValue}"/>.</summary>
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
            // HttpClient.Timeout: kalderen har ikke bedt om annullering, så det er tjenesten der er for langsom.
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

    /// <summary>Oversætter en HTTP-fejlstatus til en fejlkode.</summary>
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
    // Tjenestens JSON-form (HolidayDocument i domain/schemas.py). Holdes privat, så
    // Application ikke arver tjenestens feltnavne.
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