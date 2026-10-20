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
    private const int BookingsPerGuest = 80;
    private const int RoomsPerFloor = 20;

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

        var roomOf = PlaceInRooms(rows, out var roomCount);
        var guestOf = AssignGuests(rows, out var guestCountries);

        var placed = rows
            .Select(row => new MarketSeedPlacement(row, roomOf[row.Number], guestOf[row.Number]))
            .ToArray();

        return new MarketSeedPlan(roomCount, guestCountries, placed);
    }

    /// <summary>The room number of the room with the given 1-based number: floor and place, for example <c>101</c>.</summary>
    /// <param name="roomNumber">The 1-based number of the room in the plan.</param>
    /// <returns>The text of the room number.</returns>
    public static string RoomNumberText(int roomNumber)
    {
        var floor = ((roomNumber - 1) / RoomsPerFloor) + 1;
        var place = ((roomNumber - 1) % RoomsPerFloor) + 1;

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

    private static Dictionary<int, int> PlaceInRooms(IReadOnlyList<MarketSeedRow> rows, out int roomCount)
    {
        var roomOf = new Dictionary<int, int>(rows.Count);
        var busy = new PriorityQueue<int, (DateOnly Free, int Room)>();
        var rooms = 0;

        foreach (var row in rows.Where(row => !row.IsCanceled).OrderBy(row => row.Arrival).ThenBy(row => row.Number))
        {
            // A room is free on the day the guest in it leaves: the period is half-open (BR-39).
            int room;
            if (busy.TryPeek(out var freed, out var priority) && priority.Free <= row.Arrival)
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
        foreach (var row in rows.Where(row => row.IsCanceled))
        {
            roomOf[row.Number] = ((row.Number - 1) % roomCount) + 1;
        }

        return roomOf;
    }

    private static Dictionary<int, int> AssignGuests(IReadOnlyList<MarketSeedRow> rows, out IReadOnlyList<string> guestCountries)
    {
        var guestOf = new Dictionary<int, int>(rows.Count);
        var countries = new List<string>();

        foreach (var group in rows.GroupBy(row => row.Country, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var members = group.OrderBy(row => row.Number).ToArray();
            var guests = Math.Max(1, (members.Length + BookingsPerGuest - 1) / BookingsPerGuest);
            var first = countries.Count + 1;
            countries.AddRange(Enumerable.Repeat(group.Key, guests));

            for (var i = 0; i < members.Length; i++)
            {
                guestOf[members[i].Number] = first + (i % guests);
            }
        }

        guestCountries = countries;
        return guestOf;
    }
}
