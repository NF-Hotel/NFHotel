using Microsoft.EntityFrameworkCore;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IBookingRepository"/>.
/// </summary>
/// <remarks>
/// The repository does <b>not decide</b>: which statuses block comes as a parameter from
/// the service, which has taken them from <see cref="NFHotel.Domain.ValueObjects.BookingStatusExtensions.BlocksRoom"/>. There is no
/// hard-coded <c>WHERE status &lt;&gt; 4</c> in this file.
/// <para>
/// The date window is filtered on the generated column <c>effective_end_date</c> (A-01), read
/// via <c>EF.Property</c> because <c>Booking.EffectiveEndDate</c> is a computed C# property
/// without a column. The <i>final</i> overlap decision is always made by the domain (A-04) —
/// SQL only filters coarsely.
/// </para>
/// </remarks>
public sealed class BookingRepository : Repository<Booking, int>, IBookingRepository, IBookingQueries
{
    /// <summary>
    /// Creates the repository.
    /// </summary>
    /// <param name="context">Konteksten.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context"/> is <c>null</c>.</exception>
    public BookingRepository(HotelDbContext context)
        : base(context)
    {
    }

    // --------------------------------------------------------
    // READING — projections (A-11)
    // --------------------------------------------------------

    /// <inheritdoc />
    public async Task<BookingReadModel?> GetReadModelAsync(
        int bookingId,
        CancellationToken cancellationToken = default) =>
        await ProjectReadModels(Context.Bookings.AsNoTracking().Where(booking => booking.Id == bookingId))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<BookingReadModel>> GetOverviewAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyCollection<BookingStatus> includedStatuses,
        string? searchText,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(includedStatuses);

        var query = FilterByWindow(Context.Bookings.AsNoTracking(), periodStart, periodEnd);

        query = FilterByStatuses(query, includedStatuses);
        query = FilterBySearchText(query, searchText);

        // BR-111: the filtering happens in the database. The sorting is the service's responsibility (BR-32);
        // here only a deterministic order is ensured. The order is set on the entity, before the projection:
        // EF Core cannot translate a member of the positional read model that the query has just constructed.
        return await ProjectReadModels(query.OrderBy(booking => booking.StartDate).ThenBy(booking => booking.Id))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoomOccupancyDto>> GetOccupancyAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyCollection<BookingStatus> blockingStatuses,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blockingStatuses);

        var query = FilterByWindow(Context.Bookings.AsNoTracking(), periodStart, periodEnd);

        query = FilterByStatuses(query, blockingStatuses);

        return await query
            .Select(booking => new RoomOccupancyDto(
                booking.Id,
                booking.RoomId,
                booking.StartDate,
                EF.Property<DateOnly>(booking, BookingOverlapConstraint.EffectiveEndDateProperty),
                booking.Status))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    // --------------------------------------------------------
    // KOMMANDOSTIER — entiteter
    // --------------------------------------------------------

    /// <inheritdoc />
    public async Task<IReadOnlyList<Booking>> GetOverlapCandidatesAsync(
        int roomId,
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyCollection<BookingStatus> blockingStatuses,
        int? excludeBookingId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(blockingStatuses);

        var query = FilterByWindow(
            Context.Bookings.AsNoTracking().Where(booking => booking.RoomId == roomId),
            periodStart,
            periodEnd);

        query = FilterByStatuses(query, blockingStatuses);

        if (excludeBookingId is int excludedId)
        {
            query = query.Where(booking => booking.Id != excludedId);
        }

        return await query.ToArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Filters coarsely on the half-open date window against the <i>effective</i> period of the booking (A-01).
    /// </summary>
    /// <param name="query">The query being narrowed.</param>
    /// <param name="periodStart">The first date of the window, inclusive.</param>
    /// <param name="periodEnd">The last date of the window, exclusive.</param>
    /// <returns>The query with the window filter applied.</returns>
    /// <remarks>
    /// The predicate is half-open and mirrors <c>DateRange.Overlaps</c> (BR-39). It is still
    /// a <i>coarse filter</i>: the binding decision is made by
    /// <see cref="NFHotel.Domain.Entities.Booking.ConflictsWith"/> in the service, so SQL and C# cannot disagree
    /// om <c>&lt;</c> mod <c>&lt;=</c> (A-04).
    /// </remarks>
    private static IQueryable<Booking> FilterByWindow(
        IQueryable<Booking> query,
        DateOnly periodStart,
        DateOnly periodEnd) =>
        query.Where(booking =>
            booking.StartDate < periodEnd
            && EF.Property<DateOnly>(booking, BookingOverlapConstraint.EffectiveEndDateProperty) > periodStart);

    /// <summary>
    /// Filters on the status set the caller has chosen. An empty set means no rows.
    /// </summary>
    /// <param name="query">The query being narrowed.</param>
    /// <param name="statuses">The statuses to include.</param>
    /// <returns>The query with the status filter applied.</returns>
    private static IQueryable<Booking> FilterByStatuses(
        IQueryable<Booking> query,
        IReadOnlyCollection<BookingStatus> statuses)
    {
        var values = statuses as BookingStatus[] ?? statuses.ToArray();

        return query.Where(booking => values.Contains(booking.Status));
    }

    /// <summary>
    /// Filters on free text in the guest's name, country and email and in the room number (BR-25).
    /// </summary>
    /// <param name="query">The query being narrowed.</param>
    /// <param name="searchText">Free text, or <c>null</c>/whitespace for no filter.</param>
    /// <returns>The query with the search filter applied.</returns>
    private static IQueryable<Booking> FilterBySearchText(IQueryable<Booking> query, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return query;
        }

        var pattern = SqlPattern.Contains(searchText);

        return query.Where(booking =>
            EF.Functions.ILike(booking.Guest!.FirstName, pattern, SqlPattern.EscapeCharacter)
            || EF.Functions.ILike(booking.Guest!.LastName, pattern, SqlPattern.EscapeCharacter)
            || EF.Functions.ILike(booking.Guest!.Country, pattern, SqlPattern.EscapeCharacter)
            || EF.Functions.ILike(booking.Guest!.Email, pattern, SqlPattern.EscapeCharacter)
            || EF.Functions.ILike(booking.Room!.RoomNumber, pattern, SqlPattern.EscapeCharacter));
    }

    /// <summary>
    /// Projects bookings with room and guest directly in SQL (A-11).
    /// </summary>
    /// <param name="query">The query being projected.</param>
    /// <returns>The read models.</returns>
    /// <remarks>
    /// One place for the projection, used by both the detail and the overview lookup. Without
    /// the projection a missing <c>Include</c> would give empty fields instead of an error.
    /// The passport number is deliberately left out — <see cref="GuestListItemDto"/> carries only whether there is one
    /// (B-09).
    /// </remarks>
    private static IQueryable<BookingReadModel> ProjectReadModels(IQueryable<Booking> query) =>
        query.Select(booking => new BookingReadModel(
            booking.Id,
            booking.StartDate,
            booking.EndDate,
            EF.Property<DateOnly>(booking, BookingOverlapConstraint.EffectiveEndDateProperty),
            booking.Status,
            booking.CheckInTime,
            booking.CheckOutTime,
            booking.CheckOutDate,
            new RoomListItemDto(
                booking.Room!.Id,
                booking.Room.RoomNumber,
                booking.Room.Floor,
                booking.Room.Size,
                booking.Room.Capacity,
                booking.Room.Status,
                booking.Room.HousekeepingStatus),
            new GuestListItemDto(
                booking.Guest!.Id,
                booking.Guest.FirstName,
                booking.Guest.LastName,
                booking.Guest.FirstName + " " + booking.Guest.LastName,
                booking.Guest.Email,
                booking.Guest.PhoneNumber,
                booking.Guest.Country,
                booking.Guest.PassportNumber != null)));
}
