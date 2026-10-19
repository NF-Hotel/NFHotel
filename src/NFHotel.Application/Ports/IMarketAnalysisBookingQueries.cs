using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Application.Ports;

/// <summary>The read of the hotel bookings in the shape that the markets API takes (MIL-026).</summary>
public interface IMarketAnalysisBookingQueries
{
    /// <summary>Reads every booking with its guest country and its market details.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The bookings ordered by id; empty when the hotel has none.</returns>
    Task<IReadOnlyList<MarketBookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default);
}
