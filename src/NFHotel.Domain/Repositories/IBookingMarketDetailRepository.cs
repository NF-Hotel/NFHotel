using NFHotel.Domain.Entities;

namespace NFHotel.Domain.Repositories;

/// <summary>Stores the market details of a booking, such as the price per night it was made at (MIL-055).</summary>
public interface IBookingMarketDetailRepository : IRepository<BookingMarketDetail, int>
{
    /// <summary>
    /// Takes back a detail that was added but could not be saved, so that a later save in the same scope does not try it again.
    /// </summary>
    /// <param name="detail">The detail.</param>
    void Discard(BookingMarketDetail detail);
}
