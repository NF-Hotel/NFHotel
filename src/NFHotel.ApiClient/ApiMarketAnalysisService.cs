using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.MarketAnalysis;

namespace NFHotel.ApiClient;

/// <summary>
/// Starts the market analysis and reads its report through the API for the web application (MIL-026). The API calls
/// the data analysis service and stores the result; the web application never holds the token of that service.
/// </summary>
public sealed class ApiMarketAnalysisService : IMarketAnalysisService
{
    private readonly ApiGateway _api;

    /// <summary>Creates the client.</summary>
    /// <param name="api">The gateway that carries the tokens.</param>
    public ApiMarketAnalysisService(ApiGateway api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    public Task<Result<MarketAnalysisRunStateDto>> StartRunAsync(StartMarketAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _api.PostAsync<MarketAnalysisRunStateDto>(
            $"{ApiRoutes.MarketAnalysis}/{ApiRoutes.MarketAnalysisRuns}",
            request,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<MarketAnalysisRunStateDto>> GetRunStateAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        if (runId == Guid.Empty)
        {
            return Result<MarketAnalysisRunStateDto>.Failure(ErrorCodes.MarketAnalysisRunner.RunNotFound);
        }

        return await _api
            .GetAsync<MarketAnalysisRunStateDto>($"{ApiRoutes.MarketAnalysis}/{ApiRoutes.MarketAnalysisRuns}/{runId}", cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Result<MarketAnalysisReportDto>> GetLatestReportAsync(CancellationToken cancellationToken = default) =>
        _api.GetAsync<MarketAnalysisReportDto>($"{ApiRoutes.MarketAnalysis}/{ApiRoutes.MarketAnalysisLatest}", cancellationToken);
}
