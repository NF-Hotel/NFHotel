using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Ports;

namespace NFHotel.TestSupport;

/// <summary>The bookings of the hotel as the market analysis reads them; a test fills the list.</summary>
public sealed class FakeMarketAnalysisBookingQueries : IMarketAnalysisBookingQueries
{
    public List<MarketBookingDto> Bookings { get; } = [Booking(1), Booking(2, canceled: true)];

    public static MarketBookingDto Booking(int id, bool canceled = false) =>
        new(id, "NF Hotel", canceled, 30, new DateOnly(2019, 1, 30), new DateOnly(2019, 3, 1), 1, 2, 2, 0, 0, "BB", "Portugal", "Direct", false, 0, "C", 0, "No Deposit", "Contract", 0, 1, 94.00m);

    /// <summary>The number of rooms the hotel has.</summary>
    public int RoomCount { get; set; } = 10;

    public (DateOnly? From, DateOnly? To)? LastPeriod { get; private set; }

    public Task<int> CountRoomsAsync(CancellationToken cancellationToken = default) => Task.FromResult(RoomCount);

    public Task<IReadOnlyList<MarketBookingDto>> GetBookingsAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        LastPeriod = (from, to);
        return Task.FromResult<IReadOnlyList<MarketBookingDto>>(
            [.. Bookings.Where(booking => (from is null || booking.ArrivalDate >= from) && (to is null || booking.ArrivalDate <= to))]);
    }
}
