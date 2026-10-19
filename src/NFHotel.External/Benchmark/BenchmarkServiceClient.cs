using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.External.Config;

namespace NFHotel.External.Benchmark;

/// <summary>
/// Calls the benchmark service over HTTP with the bearer token of this application (MIL-020). Every failure is a code in
/// the <see cref="Result"/>; nothing is thrown for a service that is down, slow or refuses.
/// </summary>
public sealed class BenchmarkServiceClient : IBenchmarkServiceClient
{
    private const int MaxModels = 4;
    private const int MaxRuns = 5;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // The service delivers snake_case (run_id, queued_at), as openapi/benchmark-contract.yaml says.
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly HttpClient _httpClient;
    private readonly BenchmarkServiceOptions _options;
    private readonly ILogger<BenchmarkServiceClient> _logger;

    /// <summary>Creates the client.</summary>
    /// <param name="httpClient">The client with the address and the token set.</param>
    /// <param name="options">The settings.</param>
    /// <param name="logger">The log.</param>
    public BenchmarkServiceClient(HttpClient httpClient, BenchmarkServiceOptions options, ILogger<BenchmarkServiceClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<string>>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<ModelsWire>(HttpMethod.Get, "models", null, cancellationToken).ConfigureAwait(false);

        return result.IsFailure
            ? Result<IReadOnlyList<string>>.Failure(result.Errors)
            : result.Value.Models is null
                ? Unexpected<IReadOnlyList<string>>()
                : Result<IReadOnlyList<string>>.Success(result.Value.Models);
    }

    /// <inheritdoc />
    public async Task<Result<BenchmarkRunStateDto>> StartRunAsync(
        StartBenchmarkRunRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // The same limits as the service, so that an obvious mistake does not cost a call.
        if (request.Models is null
            || request.Models.Count is 0 or > MaxModels
            || request.Models.Distinct(StringComparer.Ordinal).Count() != request.Models.Count
            || request.Runs is < 1 or > MaxRuns
            || !(request.HotelAnalysis || request.DeterministicTasks))
        {
            return Result<BenchmarkRunStateDto>.Failure(ErrorCodes.BenchmarkRunner.InvalidRequest);
        }

        var body = new StartWire(request.Models, request.HotelAnalysis, request.DeterministicTasks, request.Runs);
        var result = await SendAsync<RunWire>(HttpMethod.Post, "runs", body, cancellationToken).ConfigureAwait(false);

        return ToState(result);
    }

    /// <inheritdoc />
    public async Task<Result<BenchmarkRunStateDto>> GetRunAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            return Result<BenchmarkRunStateDto>.Failure(ErrorCodes.BenchmarkRunner.RunNotFound);
        }

        var result = await SendAsync<RunWire>(HttpMethod.Get, $"runs/{runId}", null, cancellationToken).ConfigureAwait(false);

        return ToState(result);
    }

    private static Result<BenchmarkRunStateDto> ToState(Result<RunWire> result)
    {
        if (result.IsFailure)
        {
            return Result<BenchmarkRunStateDto>.Failure(result.Errors);
        }

        var wire = result.Value;
        return wire.RunId == Guid.Empty || wire.State is null || wire.Delivery is null
            ? Unexpected<BenchmarkRunStateDto>()
            : Result<BenchmarkRunStateDto>.Success(new BenchmarkRunStateDto(
                wire.RunId,
                wire.State,
                wire.Delivery,
                wire.Models ?? [],
                wire.QueuedAt,
                wire.StartedAt,
                wire.FinishedAt,
                wire.Error));
    }

    private static Result<TValue> Unexpected<TValue>() =>
        Result<TValue>.Failure(ErrorCodes.BenchmarkRunner.UnexpectedResponse);

    private async Task<Result<TWire>> SendAsync<TWire>(
        HttpMethod method,
        string relativeUrl,
        object? body,
        CancellationToken cancellationToken)
    {
        if (!_options.IsConfigured)
        {
            _logger.LogWarning(
                "The benchmark service is not configured: set {TokenSetting} and {BaseUrlSetting}.",
                $"{BenchmarkServiceOptions.SectionName}:{BenchmarkServiceOptions.TokenName}",
                $"{BenchmarkServiceOptions.SectionName}:{BenchmarkServiceOptions.BaseUrlName}");

            return Result<TWire>.Failure(ErrorCodes.BenchmarkRunner.NotConfigured);
        }

        try
        {
            using var request = new HttpRequestMessage(method, relativeUrl);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body, options: JsonOptions);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return await FailAsync<TWire>(method, response, cancellationToken).ConfigureAwait(false);
            }

            var wire = await response.Content.ReadFromJsonAsync<TWire>(JsonOptions, cancellationToken).ConfigureAwait(false);

            return wire is null
                ? Unexpected<TWire>()
                : Result<TWire>.Success(wire);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout: the caller did not ask for cancellation, so the service is too slow.
            _logger.LogWarning("The benchmark service {Method} timed out after {Seconds} s.", method, _options.TimeoutSeconds);

            return Result<TWire>.Failure(ErrorCodes.BenchmarkRunner.Unavailable);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "The benchmark service {Method} could not be reached.", method);

            return Result<TWire>.Failure(ErrorCodes.BenchmarkRunner.Unavailable);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "The benchmark service {Method} returned unreadable JSON.", method);

            return Result<TWire>.Failure(ErrorCodes.BenchmarkRunner.UnexpectedResponse);
        }
    }

    private async Task<Result<TWire>> FailAsync<TWire>(HttpMethod method, HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = response.StatusCode;
        var serviceCode = status == HttpStatusCode.BadRequest
            ? await ReadCodeAsync(response, cancellationToken).ConfigureAwait(false)
            : null;

        var code = status switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ErrorCodes.BenchmarkRunner.Unauthorized,
            HttpStatusCode.NotFound => ErrorCodes.BenchmarkRunner.RunNotFound,
            HttpStatusCode.TooManyRequests => ErrorCodes.BenchmarkRunner.QueueFull,
            HttpStatusCode.BadRequest when serviceCode == "model_not_installed" => ErrorCodes.BenchmarkRunner.ModelNotInstalled,
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ErrorCodes.BenchmarkRunner.InvalidRequest,
            >= HttpStatusCode.InternalServerError => ErrorCodes.BenchmarkRunner.Unavailable,
            _ => ErrorCodes.BenchmarkRunner.UnexpectedResponse,
        };

        _logger.LogWarning("The benchmark service {Method} answered HTTP {Status} ({Code}).", method, (int)status, code);

        return Result<TWire>.Failure(code);
    }

    private static async Task<string?> ReadCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemWire>(JsonOptions, cancellationToken).ConfigureAwait(false);
            return problem?.Code;
        }
        catch (JsonException)
        {
            // The status code alone decides when the body is not the problem object of the contract.
            return null;
        }
    }

    private sealed record ModelsWire(IReadOnlyList<string>? Models);

    private sealed record ProblemWire(string? Code);

    private sealed record StartWire(
        IReadOnlyList<string> Models,
        bool HotelAnalysis,
        bool DeterministicTasks,
        int Runs);

    private sealed record RunWire(
        Guid RunId,
        string? State,
        string? Delivery,
        IReadOnlyList<string>? Models,
        DateTimeOffset QueuedAt,
        DateTimeOffset? StartedAt,
        DateTimeOffset? FinishedAt,
        string? Error);
}
