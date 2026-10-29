using NFHotel.Application.Dto.MarketAnalysis;

namespace NFHotel.Application.Ports;

/// <summary>The read of the hotel bookings in the shape that the markets API takes (MIL-026).</summary>
public interface IMarketAnalysisBookingQueries
{
    /// <summary>Reads the bookings of a period with their guest country and their market details.</summary>
    /// <param name="from">The first arrival date to include, or <c>null</c> for no lower limit.</param>
    /// <param name="to">The last arrival date to include, or <c>null</c> for no upper limit.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The bookings ordered by id; empty when the hotel has none in the period.</returns>
    Task<IReadOnlyList<MarketBookingDto>> GetBookingsAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    /// <summary>Counts the rooms of the hotel, which the market analysis service needs for the occupancy.</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The number of rooms.</returns>
    Task<int> CountRoomsAsync(CancellationToken cancellationToken = default);
}
