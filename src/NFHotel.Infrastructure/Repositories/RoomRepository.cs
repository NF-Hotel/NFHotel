using Microsoft.EntityFrameworkCore;

using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.Repositories;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;

namespace NFHotel.Infrastructure.Repositories;

/// <summary>
/// EF Core-implementering af <see cref="IRoomRepository"/>.
/// </summary>
/// <remarks>
/// Filtre er parametre, ikke hardkodede valg: det gamle <c>GetAllByAvailability()</c>
/// afgjorde selv hvad "tilgængelig" betød (BR-110). Her vælger servicen.
/// </remarks>
public sealed class RoomRepository : Repository<Room, int>, IRoomRepository, IRoomQueries
{
    /// <summary>
    /// Opretter repositoryet.
    /// </summary>
    /// <param name="context">Konteksten.</param>
    /// <exception cref="ArgumentNullException">Kastes hvis <paramref name="context"/> er <c>null</c>.</exception>
    public RoomRepository(HotelDbContext context)
        : base(context)
    {
    }

    // --------------------------------------------------------
    // LÆSNING — projektioner (A-11)
    // --------------------------------------------------------

    /// <inheritdoc />
    public async Task<RoomDetailsDto?> GetDetailsAsync(int roomId, CancellationToken cancellationToken = default) =>
        await Context.Rooms
            .AsNoTracking()
            .Where(room => room.Id == roomId)
            .Select(room => new RoomDetailsDto(
                room.Id,
                room.RoomNumber,
                room.Floor,
                room.Size,
                room.Capacity,
                room.Status,
                room.HousekeepingStatus,

                // BR-110: samme udtryk som domænets Room.IsBookable. Beregnes i SQL, fordi
                // rækken ikke materialiseres som entitet på læsestien.
                room.Status == RoomStatus.Available,

                // BR-78, BR-112: sletteguarden bæres op i UI'et som en oplysning, så
                // advarslen kan vises inden knappen trykkes.
                Context.Bookings.Any(booking => booking.RoomId == room.Id)))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoomListItemDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await ProjectListItems(Context.Rooms.AsNoTracking())
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoomListItemDto>> GetByFilterAsync(
        RoomFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var query = Context.Rooms.AsNoTracking();

        // BR-74, BR-76: null betyder "filtrér ikke på dette". Et tomt filter er derfor
        // "ryd filter" — der er ingen særskilt use case for det.
        if (filter.Floor is int floor)
        {
            query = query.Where(room => room.Floor == floor);
        }

        if (filter.Size is RoomSize size)
        {
            query = query.Where(room => room.Size == size);
        }

        if (filter.Status is RoomStatus status)
        {
            query = query.Where(room => room.Status == status);
        }

        if (filter.HousekeepingStatus is HousekeepingStatus housekeepingStatus)
        {
            query = query.Where(room => room.HousekeepingStatus == housekeepingStatus);
        }

        return await ProjectListItems(query).ToArrayAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AvailableRoomDto>> GetByStatusAsync(
        RoomStatus status,
        CancellationToken cancellationToken = default) =>
        await Context.Rooms
            .AsNoTracking()
            .Where(room => room.Status == status)
            .OrderBy(room => room.RoomNumber)
            .Select(room => new AvailableRoomDto(
                room.Id,
                room.RoomNumber,
                room.Floor,
                room.Size,
                room.Capacity))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetDistinctFloorsAsync(CancellationToken cancellationToken = default) =>
        await Context.Rooms
            .AsNoTracking()
            .Select(room => room.Floor)
            .Distinct()
            .OrderBy(floor => floor)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> RoomNumberExistsAsync(
        string roomNumber,
        int? excludeRoomId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomNumber);

        var query = Context.Rooms.AsNoTracking().Where(room => room.RoomNumber == roomNumber);

        if (excludeRoomId is int excludedId)
        {
            query = query.Where(room => room.Id != excludedId);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> HasAnyBookingsAsync(int roomId, CancellationToken cancellationToken = default) =>
        await Context.Bookings
            .AsNoTracking()
            .AnyAsync(booking => booking.RoomId == roomId, cancellationToken)
            .ConfigureAwait(false);

    // --------------------------------------------------------
    // KOMMANDOSTIER — entiteter
    // --------------------------------------------------------

    /// <inheritdoc />
    public void Remove(Room room) => MarkForRemoval(room);

    /// <summary>
    /// Projicerer rum til listerækker, sorteret på værelsesnummer.
    /// </summary>
    /// <param name="query">Forespørgslen der projiceres.</param>
    /// <returns>Listerækkerne.</returns>
    private static IQueryable<RoomListItemDto> ProjectListItems(IQueryable<Room> query) =>
        query
            .OrderBy(room => room.RoomNumber)
            .Select(room => new RoomListItemDto(
                room.Id,
                room.RoomNumber,
                room.Floor,
                room.Size,
                room.Capacity,
                room.Status,
                room.HousekeepingStatus));
}