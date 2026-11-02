using Microsoft.EntityFrameworkCore;

using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>Stores the market details of the bookings (MIL-026, MIL-051).</summary>
public sealed class BookingMarketDetailRepository : Repository<BookingMarketDetail, int>, IBookingMarketDetailRepository
{
    /// <summary>Creates the repository.</summary>
    /// <param name="context">The database context.</param>
    public BookingMarketDetailRepository(HotelDbContext context)
        : base(context)
    {
    }

    /// <inheritdoc />
    public void Discard(BookingMarketDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);

        Context.Entry(detail).State = EntityState.Detached;
    }
}
