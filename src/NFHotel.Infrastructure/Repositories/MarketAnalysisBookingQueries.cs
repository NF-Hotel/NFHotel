using Microsoft.EntityFrameworkCore;

using NFHotel.Application.Dto.MarketAnalysis;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.MarketAnalysis;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>
/// Reads the hotel bookings as the input of the markets API (MIL-026). A booking without market details is read with those
/// fields empty, so it is analysed with them missing. The nights are split into weekend and week nights here, because that
/// is calendar arithmetic and not something the database stores.
/// </summary>
public sealed class MarketAnalysisBookingQueries : IMarketAnalysisBookingQueries
{
    /// <summary>The name of the hotel that the markets API is told.</summary>
    public const string HotelName = "NF Hotel";

    private readonly HotelDbContext _context;

    /// <summary>Creates the reader.</summary>
    /// <param name="context">The database context.</param>
    public MarketAnalysisBookingQueries(HotelDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MarketBookingDto>> GetBookingsAsync(DateOnly? firstArrival, DateOnly? lastArrival, CancellationToken cancellationToken = default)
    {
        var rows = await (
                from booking in _context.Bookings.AsNoTracking()
                where (firstArrival == null || booking.StartDate >= firstArrival) && (lastArrival == null || booking.StartDate <= lastArrival)
                join guest in _context.Guests.AsNoTracking() on booking.GuestId equals guest.Id
                join detail in _context.BookingMarketDetails.AsNoTracking() on booking.Id equals detail.BookingId into details
                from detail in details.DefaultIfEmpty()
                orderby booking.Id
                select new
                {
                    booking.Id,
                    booking.StartDate,
                    booking.EndDate,
                    booking.Status,
                    guest.Country,
                    Detail = detail,
                })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. rows.Select(row => ToDto(row.Id, row.StartDate, row.EndDate, row.Status, row.Country, row.Detail))];
    }

    /// <inheritdoc />
    public Task<int> CountRoomsAsync(CancellationToken cancellationToken = default) =>
        _context.Rooms.AsNoTracking().CountAsync(cancellationToken);

    private static MarketBookingDto ToDto(int id, DateOnly start, DateOnly end, BookingStatus status, string country, Domain.Entities.BookingMarketDetail? detail)
    {
        var (weekend, week) = StayNights.Split(start, end);

        return new MarketBookingDto(
            id,
            HotelName,
            status == BookingStatus.Cancelled,
            detail is null ? null : start.DayNumber - detail.BookedAt.DayNumber,
            detail?.BookedAt,
            start,
            weekend,
            week,
            detail?.Adults,
            detail?.Children,
            detail?.Babies,
            detail?.Meal,
            country,
            detail?.MarketSegment,
            detail?.IsRepeatedGuest,
            detail?.PreviousCancellations,
            detail?.RoomType,
            detail?.BookingChanges,
            detail?.DepositType,
            detail?.CustomerType,
            detail?.ParkingSpaces,
            detail?.SpecialRequests,
            detail?.PricePerNight);
    }
}
