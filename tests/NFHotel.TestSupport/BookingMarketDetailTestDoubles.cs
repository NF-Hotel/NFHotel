using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;

namespace NFHotel.TestSupport;

/// <summary>An in-memory repository of the market details of the bookings.</summary>
public sealed class InMemoryBookingMarketDetailRepository : InMemoryRepository<BookingMarketDetail>, IBookingMarketDetailRepository
{
    /// <summary>The details that were given back because their save failed.</summary>
    public List<BookingMarketDetail> Discarded { get; } = [];

    public void Discard(BookingMarketDetail detail)
    {
        Discarded.Add(detail);
        Items.Remove(detail);
    }
}
