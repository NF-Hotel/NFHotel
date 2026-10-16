using Microsoft.EntityFrameworkCore;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.Rules;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>
/// EF Core-implementering af <see cref="IBookingRepository"/>.
/// </summary>
/// <remarks>
/// Repositoryet <b>beslutter ikke</b>: hvilke statusser der spærrer kommer som parameter fra
/// servicen, som har hentet dem fra <see cref="BookingRules.BlocksRoom"/>. Der findes ikke
/// et hardkodet <c>WHERE status &lt;&gt; 4</c> i denne fil.
/// <para>
/// Datovinduet filtreres på den genererede kolonne <c>effective_end_date</c> (A-01), læst
/// via <c>EF.Property</c> fordi <c>Booking.EffectiveEndDate</c> er en beregnet C#-egenskab
/// uden kolonne. Den <i>endelige</i> overlapsafgørelse træffes altid af domænet (A-04) —
/// SQL grovfiltrerer kun.
/// </para>
/// </remarks>
public sealed class BookingRepository : Repository<Booking, int>, IBookingRepository, IBookingQueries
{
    /// <summary>
    /// Opretter repositoryet.
    /// </summary>
    /// <param name="context">Konteksten.</param>
    /// <exception cref="ArgumentNullException">Kastes hvis <paramref name="context"/> er <c>null</c>.</exception>
    public BookingRepository(HotelDbContext context)
        : base(context)
    {
    }

    // --------------------------------------------------------
    // LÆSNING — projektioner (A-11)
    // --------------------------------------------------------

    /// <inheritdoc />
    public async Task<BookingReadModel?> GetReadModelAsync(
        int bookingId,
        CancellationToken cancellationToken = default) =>
        await ProjectReadModels(Context.Bookings.AsNoTracking())
            .FirstOrDefaultAsync(readModel => readModel.BookingId == bookingId, cancellationToken)
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

        // BR-111: filtreringen sker i databasen. Sorteringen er servicens ansvar (BR-32);
        // her sikres kun en deterministisk rækkefølge.
        return await ProjectReadModels(query)
            .OrderBy(readModel => readModel.StartDate)
            .ThenBy(readModel => readModel.BookingId)
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
    /// Grovfiltrerer på det halvåbne datovindue mod bookingens <i>effektive</i> periode (A-01).
    /// </summary>
    /// <param name="query">Forespørgslen der indsnævres.</param>
    /// <param name="periodStart">Vinduets første dato, inklusiv.</param>
    /// <param name="periodEnd">Vinduets sidste dato, eksklusiv.</param>
    /// <returns>Forespørgslen med vinduesfilteret pålagt.</returns>
    /// <remarks>
    /// Prædikatet er halvåbent og spejler <c>DateRange.Overlaps</c> (BR-39). Det er stadig
    /// et <i>grovfilter</i>: den bindende afgørelse træffes af
    /// <see cref="BookingRules.Conflicts"/> i servicen, så SQL og C# ikke kan blive uenige
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
    /// Filtrerer på det statussæt kalderen har valgt. Et tomt sæt betyder ingen rækker.
    /// </summary>
    /// <param name="query">Forespørgslen der indsnævres.</param>
    /// <param name="statuses">De statusser der skal med.</param>
    /// <returns>Forespørgslen med statusfilteret pålagt.</returns>
    private static IQueryable<Booking> FilterByStatuses(
        IQueryable<Booking> query,
        IReadOnlyCollection<BookingStatus> statuses)
    {
        var values = statuses as BookingStatus[] ?? statuses.ToArray();

        return query.Where(booking => values.Contains(booking.Status));
    }

    /// <summary>
    /// Filtrerer på fritekst i gæstens navn, land og e-mail samt i værelsesnummeret (BR-25).
    /// </summary>
    /// <param name="query">Forespørgslen der indsnævres.</param>
    /// <param name="searchText">Fritekst, eller <c>null</c>/whitespace for intet filter.</param>
    /// <returns>Forespørgslen med søgefilteret pålagt.</returns>
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
    /// Projicerer bookinger med rum og gæst direkte i SQL (A-11).
    /// </summary>
    /// <param name="query">Forespørgslen der projiceres.</param>
    /// <returns>Læsemodellerne.</returns>
    /// <remarks>
    /// Ét sted for projektionen, brugt af både detalje- og oversigtsopslaget. Uden
    /// projektionen ville en manglende <c>Include</c> give tomme felter i stedet for en fejl.
    /// Pasnummeret udelades bevidst — <see cref="GuestListItemDto"/> bærer kun om der er ét
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