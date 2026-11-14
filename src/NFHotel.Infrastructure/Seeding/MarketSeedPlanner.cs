using System.Globalization;

using NFHotel.Domain.ValueObjects;

namespace NFHotel.Infrastructure.Seeding;

/// <summary>
/// Decides which room and which guest every fake booking gets (MIL-026). The rule of the database is that two bookings that
/// block a room cannot overlap, so the planner places the bookings that are not cancelled in rooms like a timetable: a booking
/// goes to a room that is free on its arrival day, and a new room is made only when none is. The number of rooms is then the
/// most bookings that are at the hotel on one night. The result depends only on the rows, so the same file gives the same data.
/// </summary>
public static class MarketSeedPlanner
{
    #region Fields

    private const int BookingsPerGuest = 80;
    private const int RoomsPerFloor = 20;

    #endregion Fields

    #region Public methods

    /// <summary>Plans the rooms, the guests and the place of every booking.</summary>
    /// <param name="rows">The rows of the file.</param>
    /// <returns>The plan.</returns>
    /// <exception cref="ArgumentException">Two rows have the same booking number.</exception>
    public static MarketSeedPlan Plan(IReadOnlyList<MarketSeedRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        if (rows.Select(row => row.Number).Distinct().Count() != rows.Count)
        {
            throw new ArgumentException("Two rows have the same booking number.", nameof(rows));
        }

        Dictionary<int, int> roomOf = PlaceInRooms(rows, out int roomCount);
        Dictionary<int, int> guestOf = AssignGuests(rows, out IReadOnlyList<string> guestCountries);

        MarketSeedPlacement[] placed = rows
            .Select(row => new MarketSeedPlacement(row, roomOf[row.Number], guestOf[row.Number]))
            .ToArray();

        return new MarketSeedPlan(roomCount, guestCountries, placed);
    }

    /// <summary>The room number of the room with the given 1-based number: floor and place, for example <c>101</c>.</summary>
    /// <param name="roomNumber">The 1-based number of the room in the plan.</param>
    /// <returns>The text of the room number.</returns>
    public static string RoomNumberText(int roomNumber)
    {
        int floor = ((roomNumber - 1) / RoomsPerFloor) + 1;
        int place = ((roomNumber - 1) % RoomsPerFloor) + 1;

        return string.Create(CultureInfo.InvariantCulture, $"{floor}{place:00}");
    }

    /// <summary>The floor of the room with the given 1-based number.</summary>
    /// <param name="roomNumber">The 1-based number of the room in the plan.</param>
    /// <returns>The floor, from 1.</returns>
    public static int FloorOf(int roomNumber) => ((roomNumber - 1) / RoomsPerFloor) + 1;

    /// <summary>The size of the room with the given 1-based number: single, double and suite in turn.</summary>
    /// <param name="roomNumber">The 1-based number of the room in the plan.</param>
    /// <returns>The size.</returns>
    public static RoomSize SizeOf(int roomNumber) => (RoomSize)((roomNumber - 1) % 3);

    /// <summary>The number of guests the room takes, by its size.</summary>
    /// <param name="size">The size.</param>
    /// <returns>The capacity.</returns>
    public static int CapacityOf(RoomSize size) => size switch
    {
        RoomSize.Single => 1,
        RoomSize.Double => 2,
        _ => 4,
    };

    #endregion Public methods

    #region Private methods

    private static Dictionary<int, int> PlaceInRooms(IReadOnlyList<MarketSeedRow> rows, out int roomCount)
    {
        Dictionary<int, int> roomOf = new Dictionary<int, int>(rows.Count);
        PriorityQueue<int, (DateOnly Free, int Room)> busy = new PriorityQueue<int, (DateOnly Free, int Room)>();
        int rooms = 0;

        foreach (MarketSeedRow row in rows.Where(row => !row.IsCanceled).OrderBy(row => row.Arrival).ThenBy(row => row.Number))
        {
            // A room is free on the day the guest in it leaves: the period is half-open (BR-39).
            int room;
            if (busy.TryPeek(out int freed, out (DateOnly Free, int Room) priority) && priority.Free <= row.Arrival)
            {
                busy.Dequeue();
                room = freed;
            }
            else
            {
                room = ++rooms;
            }

            roomOf[row.Number] = room;
            busy.Enqueue(room, (row.Departure, room));
        }

        roomCount = Math.Max(1, rooms);

        // A cancelled booking blocks nothing, so any room will do; the booking number spreads them.
        foreach (MarketSeedRow row in rows.Where(row => row.IsCanceled))
        {
            roomOf[row.Number] = ((row.Number - 1) % roomCount) + 1;
        }

        return roomOf;
    }

    private static Dictionary<int, int> AssignGuests(IReadOnlyList<MarketSeedRow> rows, out IReadOnlyList<string> guestCountries)
    {
        Dictionary<int, int> guestOf = new Dictionary<int, int>(rows.Count);
        List<string> countries = [];

        foreach (IGrouping<string, MarketSeedRow> group in rows.GroupBy(row => row.Country, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            MarketSeedRow[] members = group.OrderBy(row => row.Number).ToArray();
            int guests = Math.Max(1, (members.Length + BookingsPerGuest - 1) / BookingsPerGuest);
            int first = countries.Count + 1;
            countries.AddRange(Enumerable.Repeat(group.Key, guests));

            for (int i = 0; i < members.Length; i++)
            {
                guestOf[members[i].Number] = first + (i % guests);
            }
        }

        guestCountries = countries;
        return guestOf;
    }

    #endregion Private methods
}
