using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.UseCases;

namespace NFHotel.Application.Ports;

/// <summary>
/// The markets API (repository <c>Notebook_Market_Analysis</c>, deployed in MIL-004) as the website API calls it: send the
/// bookings and get the six analyses with the AI insights. The call is synchronous and with insights it takes up to twelve
/// minutes, so the caller runs it in the background.
/// </summary>
public interface IMarketAnalysisServiceClient
{
    /// <summary>Runs the analyses on a list of bookings, with the AI insights.</summary>
    /// <param name="bookings">The bookings; at least one.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The result, or a code: input rejected, unavailable, not configured, unexpected response.</returns>
    Task<Result<MarketAnalysisResultDto>> AnalyzeAsync(
        IReadOnlyList<MarketBookingDto> bookings,
        CancellationToken cancellationToken = default);
}
