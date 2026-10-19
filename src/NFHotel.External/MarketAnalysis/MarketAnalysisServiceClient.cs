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
/// Calls the data analysis service over HTTP with the bearer token of this application (MIL-026). Every failure is a
/// code in the <see cref="Result"/>; nothing is thrown for a service that is down, slow or refuses.
/// </summary>
public sealed class MarketAnalysisServiceClient : IMarketAnalysisServiceClient
{
    private const string StartPath = "api/v1/notebooks/market_analysis/runs";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // The service delivers snake_case (job_id, started_at), as its docs/api.md says.
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _httpClient;
    private readonly MarketAnalysisServiceOptions _options;
    private readonly ILogger<MarketAnalysisServiceClient> _logger;

    /// <summary>Creates the client.</summary>
    /// <param name="httpClient">The client with the address and the token set.</param>
    /// <param name="options">The settings.</param>
    /// <param name="logger">The log.</param>
    public MarketAnalysisServiceClient(
        HttpClient httpClient,
        MarketAnalysisServiceOptions options,
        ILogger<MarketAnalysisServiceClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<MarketAnalysisRunStateDto>> StartRunAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<RunWire>(HttpMethod.Post, StartPath, cancellationToken).ConfigureAwait(false);

        return ToState(result);
    }

    /// <inheritdoc />
    public async Task<Result<MarketAnalysisRunStateDto>> GetRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            return Result<MarketAnalysisRunStateDto>.Failure(ErrorCodes.MarketAnalysisRunner.RunNotFound);
        }

        var result = await SendAsync<RunWire>(HttpMethod.Get, $"api/v1/runs/{runId}", cancellationToken).ConfigureAwait(false);

        return ToState(result);
    }

    /// <inheritdoc />
    public async Task<Result<MarketAnalysisResultDto>> GetResultAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            return Result<MarketAnalysisResultDto>.Failure(ErrorCodes.MarketAnalysisRunner.RunNotFound);
        }

        var result = await SendAsync<ResultWire>(HttpMethod.Get, $"api/v1/runs/{runId}/result", cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            return Result<MarketAnalysisResultDto>.Failure(result.Errors);
        }

        var wire = result.Value;
        return wire.Metadata is null || wire.SchemaVersion is null || wire.Content is null
            ? Unexpected<MarketAnalysisResultDto>()
            : Result<MarketAnalysisResultDto>.Success(new MarketAnalysisResultDto(
                runId,
                wire.SchemaVersion.Value,
                wire.Metadata.StartedAt ?? default,
                wire.Metadata.FinishedAt ?? default,
                wire.Metadata.Provider,
                wire.Metadata.Model,
                new MarketAnalysisContentDto(
                    wire.Content.Summary ?? string.Empty,
                    wire.Content.Figures ?? [],
                    wire.Content.Series ?? [])));
    }

    private static Result<MarketAnalysisRunStateDto> ToState(Result<RunWire> result)
    {
        if (result.IsFailure)
        {
            return Result<MarketAnalysisRunStateDto>.Failure(result.Errors);
        }

        var wire = result.Value;
        return wire.JobId == Guid.Empty || string.IsNullOrEmpty(wire.Status)
            ? Unexpected<MarketAnalysisRunStateDto>()
            : Result<MarketAnalysisRunStateDto>.Success(new MarketAnalysisRunStateDto(
                wire.JobId,
                wire.Status,
                wire.StartedAt,
                wire.FinishedAt,
                ReadError(wire.Error)));
    }

    // The error of a failed run is a sanitised message, as a string or as an object with a message or a code.
    private static string? ReadError(JsonElement? error)
    {
        if (error is not { } element)
        {
            return null;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Object when element.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String =>
                message.GetString(),
            JsonValueKind.Object when element.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String =>
                code.GetString(),
            _ => null,
        };
    }

    private static Result<TValue> Unexpected<TValue>() =>
        Result<TValue>.Failure(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse);

    private async Task<Result<TWire>> SendAsync<TWire>(HttpMethod method, string relativeUrl, CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning(
                "The data analysis service is not configured: set {TokenSetting} and {BaseUrlSetting}.",
                $"{MarketAnalysisServiceOptions.SectionName}:{MarketAnalysisServiceOptions.TokenName}",
                $"{MarketAnalysisServiceOptions.SectionName}:{MarketAnalysisServiceOptions.BaseUrlName}");

            return Result<TWire>.Failure(ErrorCodes.MarketAnalysisRunner.NotConfigured);
        }

        try
        {
            using var request = new HttpRequestMessage(method, relativeUrl);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Fail<TWire>(method, response.StatusCode);
            }

            var wire = await response.Content.ReadFromJsonAsync<TWire>(JsonOptions, cancellationToken).ConfigureAwait(false);

            return wire is null
                ? Unexpected<TWire>()
                : Result<TWire>.Success(wire);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout: the caller did not ask for cancellation, so the service is too slow.
            _logger.LogWarning("The data analysis service {Method} timed out after {Seconds} s.", method, _options.TimeoutSeconds);

            return Result<TWire>.Failure(ErrorCodes.MarketAnalysisRunner.Unavailable);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "The data analysis service {Method} could not be reached.", method);

            return Result<TWire>.Failure(ErrorCodes.MarketAnalysisRunner.Unavailable);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "The data analysis service {Method} returned unreadable JSON.", method);

            return Result<TWire>.Failure(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse);
        }
    }

    private Result<TWire> Fail<TWire>(HttpMethod method, HttpStatusCode status)
    {
        var code = status switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ErrorCodes.MarketAnalysisRunner.Unauthorized,
            HttpStatusCode.NotFound => ErrorCodes.MarketAnalysisRunner.RunNotFound,
            HttpStatusCode.Conflict => ErrorCodes.MarketAnalysisRunner.AlreadyRunning,
            HttpStatusCode.TooManyRequests => ErrorCodes.MarketAnalysisRunner.RateLimited,
            >= HttpStatusCode.InternalServerError => ErrorCodes.MarketAnalysisRunner.Unavailable,
            _ => ErrorCodes.MarketAnalysisRunner.UnexpectedResponse,
        };

        _logger.LogWarning("The data analysis service {Method} answered HTTP {Status} ({Code}).", method, (int)status, code);

        return Result<TWire>.Failure(code);
    }

    private sealed record RunWire(
        Guid JobId,
        string? Status,
        DateTimeOffset? StartedAt,
        DateTimeOffset? FinishedAt,
        JsonElement? Error);

    private sealed record ResultWire(
        MetadataWire? Metadata,
        int? SchemaVersion,
        [property: JsonPropertyName("result")] ContentWire? Content);

    private sealed record MetadataWire(DateTimeOffset? StartedAt, DateTimeOffset? FinishedAt, string? Provider, string? Model);

    private sealed record ContentWire(
        string? Summary,
        IReadOnlyList<MarketAnalysisFigureDto>? Figures,
        IReadOnlyList<MarketAnalysisSeriesDto>? Series);
}
