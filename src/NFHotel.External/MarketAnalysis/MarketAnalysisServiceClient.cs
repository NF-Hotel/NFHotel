using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.External.Config;

namespace NFHotel.External.MarketAnalysis;

/// <summary>
/// Calls the run API of the market analysis service over HTTP (MIL-026, contract <c>openapi/market-contract.yaml</c> of
/// <c>notebook-market-analysis</c>): <c>POST /api/v1/market-analysis/runs?insights=true</c> starts a run, <c>GET .../runs/{id}</c>
/// is followed until the state is <c>completed</c> or <c>failed</c>, and <c>GET .../runs/{id}/result</c> gives result schema 1.1.
/// Every call carries the header <c>X-API-Key</c> and has a short timeout (the one of the <see cref="HttpClient"/>); the poll
/// loop has its own deadline. Every failure is a code in the <see cref="Result"/>; nothing is thrown for a service that is
/// down, slow or refuses. The API key is never logged.
/// </summary>
public sealed class MarketAnalysisServiceClient : IMarketAnalysisServiceClient
{
    /// <summary>The header that carries the API key.</summary>
    public const string ApiKeyHeader = "X-API-Key";

    private const string RunsPath = "api/v1/market-analysis/runs";
    private const string ProblemRunActive = "run_active";
    private const int MaxConsecutivePollFailures = 3;
    private const int MaxErrorCodeLength = 64;

    // The input contract is snake_case; a value that is not known is left out, which the service counts as missing.
    private static readonly JsonSerializerOptions RequestOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly MarketAnalysisServiceOptions _options;
    private readonly ILogger<MarketAnalysisServiceClient> _logger;
    private readonly TimeProvider _time;

    /// <summary>Creates the client.</summary>
    /// <param name="httpClient">The client with the address set and a short timeout.</param>
    /// <param name="options">The settings.</param>
    /// <param name="logger">The log.</param>
    /// <param name="timeProvider">The clock and the timers of the poll loop; the system clock when not given.</param>
    public MarketAnalysisServiceClient(
        HttpClient httpClient,
        MarketAnalysisServiceOptions options,
        ILogger<MarketAnalysisServiceClient> logger,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        _time = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<Result<MarketAnalysisResultDto>> AnalyzeAsync(
        MarketAnalysisRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_options.IsConfigured)
        {
            _logger.LogWarning(
                "The market analysis service is not configured: set {BaseUrlSetting}.",
                $"{MarketAnalysisServiceOptions.SectionName}:{MarketAnalysisServiceOptions.BaseUrlName}");

            return Result<MarketAnalysisResultDto>.Failure(ErrorCodes.MarketAnalysisRunner.NotConfigured);
        }

        try
        {
            var started = await StartAsync(request, cancellationToken).ConfigureAwait(false);
            if (started.IsFailure)
            {
                return Result<MarketAnalysisResultDto>.Failure(started.Errors[0].Code);
            }

            var runId = started.Value;
            _logger.LogInformation("The market analysis service queued run {RunId}.", runId);

            var finished = await WaitAsync(runId, cancellationToken).ConfigureAwait(false);
            return finished.IsFailure
                ? Result<MarketAnalysisResultDto>.Failure(finished.Errors[0].Code)
                : await ReadResultAsync(runId, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout: the caller did not ask for cancellation, so one call took longer than the limit.
            _logger.LogWarning("A call to the market analysis service timed out after {Seconds} s.", _options.TimeoutSeconds);

            return Result<MarketAnalysisResultDto>.Failure(ErrorCodes.MarketAnalysisRunner.Unavailable);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "The market analysis service could not be reached.");

            return Result<MarketAnalysisResultDto>.Failure(ErrorCodes.MarketAnalysisRunner.Unavailable);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "The market analysis service returned unreadable JSON.");

            return Result<MarketAnalysisResultDto>.Failure(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse);
        }
    }

    private async Task<Result<Guid>> StartAsync(MarketAnalysisRunRequest request, CancellationToken cancellationToken)
    {
        // The period is sent only when both dates are chosen; without it the service takes the span of the bookings.
        var period = request.From is { } first && request.To is { } last ? new PeriodBody(first, last) : null;
        var body = new RunBody(request.RoomCount, period, request.Bookings);

        using var message = NewRequest(HttpMethod.Post, $"{RunsPath}?insights=true");
        message.Content = JsonContent.Create(body, options: RequestOptions);
        using var response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return Result<Guid>.Failure(await FailAsync(response, "start", cancellationToken).ConfigureAwait(false));
        }

        var state = await ReadStateAsync(response, cancellationToken).ConfigureAwait(false);
        return state is { RunId: { } runId }
            ? Result<Guid>.Success(runId)
            : Result<Guid>.Failure(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse);
    }

    // Asks for the state of the run until it is completed or failed, until the deadline, or until the caller cancels.
    private async Task<Result> WaitAsync(Guid runId, CancellationToken cancellationToken)
    {
        var deadline = _time.GetUtcNow().AddSeconds(_options.RunDeadlineSeconds);
        var interval = TimeSpan.FromSeconds(_options.PollIntervalSeconds);
        var failures = 0;

        while (true)
        {
            var transient = false;
            try
            {
                var state = await GetStateAsync(runId, cancellationToken).ConfigureAwait(false);
                if (state.IsFailure)
                {
                    var code = state.Errors[0].Code;
                    if (code != ErrorCodes.MarketAnalysisRunner.Unavailable)
                    {
                        return Result.Failure(code);
                    }

                    transient = true;
                }
                else
                {
                    failures = 0;
                    switch (state.Value.State)
                    {
                        case MarketAnalysisRunStates.Completed:
                            return Result.Success();
                        case MarketAnalysisRunStates.Failed:
                            return Result.Failure(RunFailedCode(state.Value.ErrorCode));
                        case MarketAnalysisRunStates.Queued or MarketAnalysisRunStates.Running:
                            break;
                        default:
                            return Result.Failure(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse);
                    }
                }
            }
            catch (Exception exception) when (IsTransient(exception, cancellationToken))
            {
                transient = true;
            }

            // One lost answer is not the end of a run that takes minutes; ask again at the next interval, but not for ever.
            if (transient && ++failures >= MaxConsecutivePollFailures)
            {
                _logger.LogWarning("Asking for the state of run {RunId} failed {Count} times in a row.", runId, failures);

                return Result.Failure(ErrorCodes.MarketAnalysisRunner.Unavailable);
            }

            if (_time.GetUtcNow() >= deadline)
            {
                _logger.LogWarning("Run {RunId} did not end within {Seconds} s; it is no longer followed.", runId, _options.RunDeadlineSeconds);

                return Result.Failure(ErrorCodes.MarketAnalysisRunner.Unavailable);
            }

            await Task.Delay(interval, _time, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<Result<RunState>> GetStateAsync(Guid runId, CancellationToken cancellationToken)
    {
        using var message = NewRequest(HttpMethod.Get, $"{RunsPath}/{runId}");
        using var response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return Result<RunState>.Failure(await FailAsync(response, "state", cancellationToken).ConfigureAwait(false));
        }

        return await ReadStateAsync(response, cancellationToken).ConfigureAwait(false) is { } state
            ? Result<RunState>.Success(state)
            : Result<RunState>.Failure(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse);
    }

    private async Task<Result<MarketAnalysisResultDto>> ReadResultAsync(Guid runId, CancellationToken cancellationToken)
    {
        using var message = NewRequest(HttpMethod.Get, $"{RunsPath}/{runId}/result");
        using var response = await _httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return Result<MarketAnalysisResultDto>.Failure(await FailAsync(response, "result", cancellationToken).ConfigureAwait(false));
        }

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return MarketAnalysisResultMapper.Map(document.RootElement) is { } result
            ? Result<MarketAnalysisResultDto>.Success(result)
            : Result<MarketAnalysisResultDto>.Failure(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse);
    }

    private HttpRequestMessage NewRequest(HttpMethod method, string path)
    {
        var message = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            message.Headers.Add(ApiKeyHeader, _options.ApiKey);
        }

        return message;
    }

    private static async Task<RunState?> ReadStateAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("state", out var state)
            || state.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var runId = root.TryGetProperty("run_id", out var id) && id.ValueKind == JsonValueKind.String && Guid.TryParse(id.GetString(), out var parsed)
            ? parsed
            : default(Guid?);
        var errorCode = root.TryGetProperty("error_code", out var code) && code.ValueKind == JsonValueKind.String ? code.GetString() : null;

        return new RunState(runId, state.GetString() ?? string.Empty, errorCode);
    }

    // The problem body is {"code": "...", "message": "..."}; only the code is read, and only to tell two 409 answers apart.
    private async Task<string> FailAsync(HttpResponseMessage response, string call, CancellationToken cancellationToken)
    {
        var problem = await ReadProblemCodeAsync(response, cancellationToken).ConfigureAwait(false);
        var code = ErrorCodeOf(response.StatusCode, problem);

        _logger.LogWarning(
            "The market analysis service answered HTTP {Status} ({Problem}) to the {Call} call: {Code}.",
            (int)response.StatusCode,
            problem ?? "no problem code",
            call,
            code);

        return code;
    }

    private static string ErrorCodeOf(HttpStatusCode status, string? problem) => status switch
    {
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ErrorCodes.MarketAnalysisRunner.Unauthorized,
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ErrorCodes.MarketAnalysisRunner.InputRejected,
        HttpStatusCode.Conflict when problem == ProblemRunActive => ErrorCodes.MarketAnalysisRunner.AlreadyRunning,
        >= HttpStatusCode.InternalServerError => ErrorCodes.MarketAnalysisRunner.Unavailable,
        _ => ErrorCodes.MarketAnalysisRunner.UnexpectedResponse,
    };

    private static async Task<string?> ReadProblemCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("code", out var code)
                && code.ValueKind == JsonValueKind.String
                    ? code.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // A timeout of one call, a connection that fails and an answer of the server side (5xx) may pass; the next ask can work.
    private static bool IsTransient(Exception exception, CancellationToken cancellationToken) =>
        exception switch
        {
            OperationCanceledException => !cancellationToken.IsCancellationRequested,
            HttpRequestException => true,
            _ => false,
        };

    // The service's error_code is shown to the Manager, so it keeps only letters, digits, underscore, dot and hyphen.
    private static string RunFailedCode(string? errorCode)
    {
        var clean = new string([.. (errorCode ?? string.Empty).Where(IsCodeCharacter).Take(MaxErrorCodeLength)]);

        return clean.Length == 0
            ? ErrorCodes.MarketAnalysisRunner.RunFailed
            : string.Create(CultureInfo.InvariantCulture, $"{ErrorCodes.MarketAnalysisRunner.RunFailed}:{clean}");
    }

    private static bool IsCodeCharacter(char character) =>
        char.IsAsciiLetterOrDigit(character) || character is '_' or '.' or '-';

    private sealed record PeriodBody(DateOnly FirstDate, DateOnly LastDate);

    private sealed record RunBody(int RoomCount, PeriodBody? Period, IReadOnlyList<MarketBookingDto> Bookings);

    private sealed record RunState(Guid? RunId, string State, string? ErrorCode);
}
