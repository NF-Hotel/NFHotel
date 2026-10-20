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
/// Calls the markets API over HTTP (MIL-026): <c>POST /analyze?insights=true</c> with the bookings as a JSON array, and reads
/// result schema 1.x. Every failure is a code in the <see cref="Result"/>; nothing is thrown for a service that is down, slow
/// or refuses. The call lasts as long as the models need, up to twelve minutes (ADR-0013 of the markets API), so the client
/// is made with a long timeout and the caller runs it in the background.
/// </summary>
public sealed class MarketAnalysisServiceClient : IMarketAnalysisServiceClient
{
    private const string AnalyzePath = "analyze?insights=true";

    // The input contract is snake_case; a value that is not known is left out, which the markets API counts as missing.
    private static readonly JsonSerializerOptions RequestOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly HttpClient _httpClient;
    private readonly MarketAnalysisServiceOptions _options;
    private readonly ILogger<MarketAnalysisServiceClient> _logger;

    /// <summary>Creates the client.</summary>
    /// <param name="httpClient">The client with the address set and a long timeout.</param>
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
    public async Task<Result<MarketAnalysisResultDto>> AnalyzeAsync(
        IReadOnlyList<MarketBookingDto> bookings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bookings);

        if (!_options.IsConfigured)
        {
            _logger.LogWarning(
                "The markets API is not configured: set {BaseUrlSetting}.",
                $"{MarketAnalysisServiceOptions.SectionName}:{MarketAnalysisServiceOptions.BaseUrlName}");

            return Result<MarketAnalysisResultDto>.Failure(ErrorCodes.MarketAnalysisRunner.NotConfigured);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, AnalyzePath)
            {
                Content = JsonContent.Create(bookings, options: RequestOptions),
            };
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return Fail(response.StatusCode);
            }

            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return MarketAnalysisResultMapper.Map(document.RootElement) is { } result
                ? Result<MarketAnalysisResultDto>.Success(result)
                : Result<MarketAnalysisResultDto>.Failure(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout: the caller did not ask for cancellation, so the models took longer than the limit.
            _logger.LogWarning("The markets API timed out after {Seconds} s.", _options.TimeoutSeconds);

            return Result<MarketAnalysisResultDto>.Failure(ErrorCodes.MarketAnalysisRunner.Unavailable);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "The markets API could not be reached.");

            return Result<MarketAnalysisResultDto>.Failure(ErrorCodes.MarketAnalysisRunner.Unavailable);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "The markets API returned unreadable JSON.");

            return Result<MarketAnalysisResultDto>.Failure(ErrorCodes.MarketAnalysisRunner.UnexpectedResponse);
        }
    }

    private Result<MarketAnalysisResultDto> Fail(HttpStatusCode status)
    {
        var code = status switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ErrorCodes.MarketAnalysisRunner.Unauthorized,
            HttpStatusCode.UnprocessableEntity => ErrorCodes.MarketAnalysisRunner.InputRejected,
            >= HttpStatusCode.InternalServerError => ErrorCodes.MarketAnalysisRunner.Unavailable,
            _ => ErrorCodes.MarketAnalysisRunner.UnexpectedResponse,
        };

        _logger.LogWarning("The markets API answered HTTP {Status} ({Code}).", (int)status, code);

        return Result<MarketAnalysisResultDto>.Failure(code);
    }
}
