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

        // A booking made in the hotel holds only its price (MIL-055): its guests, meal and so on are not known, and the
        // zeros of the row would count as "0 guests", "not a repeated guest" and "no special requests" in the analysis.
        Domain.Entities.BookingMarketDetail? traits = detail is { } known && !IsPriceOnly(known) ? known : null;

        return new MarketBookingDto(
            id,
            HotelName,
            status == BookingStatus.Cancelled,
            detail is null ? null : start.DayNumber - detail.BookedAt.DayNumber,
            detail?.BookedAt,
            start,
            weekend,
            week,
            traits?.Adults,
            traits?.Children,
            traits?.Babies,
            traits?.Meal,
            country,
            traits?.MarketSegment,
            traits?.IsRepeatedGuest,
            traits?.PreviousCancellations,
            traits?.RoomType,
            traits?.BookingChanges,
            traits?.DepositType,
            traits?.CustomerType,
            traits?.ParkingSpaces,
            traits?.SpecialRequests,
            detail?.PricePerNight);
    }

    /// <summary>True when a row holds nothing but the date and the price: every count 0, the flag off and every text empty.</summary>
    private static bool IsPriceOnly(Domain.Entities.BookingMarketDetail detail) =>
        detail.Adults == 0
        && detail.Children == 0
        && detail.Babies == 0
        && detail.PreviousCancellations == 0
        && detail.BookingChanges == 0
        && detail.SpecialRequests == 0
        && detail.ParkingSpaces == 0
        && !detail.IsRepeatedGuest
        && detail.Meal is null
        && detail.MarketSegment is null
        && detail.CustomerType is null
        && detail.DepositType is null
        && detail.RoomType is null;
}
