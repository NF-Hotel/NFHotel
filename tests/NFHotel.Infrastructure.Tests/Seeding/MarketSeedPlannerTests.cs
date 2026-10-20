using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Seeding;

namespace NFHotel.Infrastructure.Tests.Seeding;

/// <summary>The rooms and guests of the fake bookings: the overlap rule must hold and the result must not change between runs (MIL-026).</summary>
public sealed class MarketSeedPlannerTests
{
    private static readonly DateOnly Day = new(2019, 3, 1);

    #region Rooms

    /// <summary>Positive: two bookings that overlap get two rooms, and the room count is the most at the hotel on one night.</summary>
    [Fact]
    public void Plan_TwoOverlappingBookings_MakesTwoRooms()
    {
        // Act
        var plan = MarketSeedPlanner.Plan([Row(1, 0, 3), Row(2, 1, 3)]);

        // Assert
        Assert.Equal(2, plan.RoomCount);
        Assert.NotEqual(plan.Bookings[0].Room, plan.Bookings[1].Room);
    }

    /// <summary>Boundary: a guest who arrives the day another leaves takes the same room, because the period is half-open.</summary>
    [Fact]
    public void Plan_ArrivalOnTheDepartureDay_ReusesTheRoom()
    {
        // Act
        var plan = MarketSeedPlanner.Plan([Row(1, 0, 3), Row(2, 3, 2)]);

        // Assert
        Assert.Equal(1, plan.RoomCount);
        Assert.Equal(plan.Bookings[0].Room, plan.Bookings[1].Room);
    }

    /// <summary>Boundary: a booking that arrives the day before the other leaves cannot share the room.</summary>
    [Fact]
    public void Plan_ArrivalTheDayBeforeDeparture_NeedsAnotherRoom()
    {
        // Act
        var plan = MarketSeedPlanner.Plan([Row(1, 0, 3), Row(2, 2, 2)]);

        // Assert
        Assert.Equal(2, plan.RoomCount);
    }

    /// <summary>Negative: a cancelled booking blocks nothing, so it does not make a room.</summary>
    [Fact]
    public void Plan_CancelledBookingOnly_NeedsOneRoomAndKeepsItsPlace()
    {
        // Act
        var plan = MarketSeedPlanner.Plan([Row(1, 0, 3, canceled: true), Row(2, 0, 3, canceled: true)]);

        // Assert
        Assert.Equal(1, plan.RoomCount);
        Assert.All(plan.Bookings, placement => Assert.Equal(1, placement.Room));
    }

    /// <summary>State: in a bigger plan no two bookings that are not cancelled overlap in one room, and the rooms are the peak.</summary>
    [Fact]
    public void Plan_ManyBookings_NeverOverlapsInARoomAndUsesThePeak()
    {
        // Arrange
        var random = new Random(7);
        var rows = Enumerable.Range(1, 600)
            .Select(number => Row(number, random.Next(0, 120), random.Next(1, 8), canceled: number % 5 == 0))
            .ToArray();

        // Act
        var plan = MarketSeedPlanner.Plan(rows);

        // Assert
        var kept = plan.Bookings.Where(placement => !placement.Row.IsCanceled).ToArray();
        foreach (var room in kept.GroupBy(placement => placement.Room))
        {
            var ordered = room.OrderBy(placement => placement.Row.Arrival).ToArray();
            for (var i = 1; i < ordered.Length; i++)
            {
                Assert.True(ordered[i - 1].Row.Departure <= ordered[i].Row.Arrival, $"room {room.Key} overlaps");
            }
        }

        Assert.Equal(PeakOccupancy(kept.Select(placement => placement.Row)), plan.RoomCount);
    }

    #endregion

    #region Repeatability and guests

    /// <summary>State: the same rows give the same plan, so a second run loads the same data.</summary>
    [Fact]
    public void Plan_SameRowsTwice_GivesTheSamePlan()
    {
        // Arrange
        var rows = Enumerable.Range(1, 50).Select(number => Row(number, number % 9, 1 + (number % 4))).ToArray();

        // Act
        var first = MarketSeedPlanner.Plan(rows);
        var second = MarketSeedPlanner.Plan(rows);

        // Assert
        Assert.Equal(first.Bookings.Select(placement => (placement.Row.Number, placement.Room, placement.Guest)), second.Bookings.Select(placement => (placement.Row.Number, placement.Room, placement.Guest)));
        Assert.Equal(first.GuestCountries, second.GuestCountries);
    }

    /// <summary>Positive: every country has guests, 80 bookings to a guest at most, and a booking gets a guest of its own country.</summary>
    [Fact]
    public void Plan_Countries_GetGuestsOfTheirOwnCountry()
    {
        // Arrange
        var rows = Enumerable.Range(1, 170).Select(number => Row(number, number, 1, country: number <= 161 ? "Unknown" : "Portugal")).ToArray();

        // Act
        var plan = MarketSeedPlanner.Plan(rows);

        // Assert
        Assert.Equal(["Portugal", "Unknown", "Unknown", "Unknown"], plan.GuestCountries);
        Assert.All(plan.Bookings, placement => Assert.Equal(placement.Row.Country, plan.GuestCountries[placement.Guest - 1]));
    }

    /// <summary>Negative: two rows with the same booking number are refused.</summary>
    [Fact]
    public void Plan_DuplicateNumber_Throws()
    {
        // Act and assert
        Assert.Throws<ArgumentException>(() => MarketSeedPlanner.Plan([Row(1, 0, 1), Row(1, 2, 1)]));
    }

    /// <summary>Boundary: no rows still make one room, so the table is not empty.</summary>
    [Fact]
    public void Plan_NoRows_MakesOneRoomAndNoBooking()
    {
        // Act
        var plan = MarketSeedPlanner.Plan([]);

        // Assert
        Assert.Equal(1, plan.RoomCount);
        Assert.Empty(plan.Bookings);
    }

    /// <summary>Positive: the room numbers have a floor and a place, and the sizes turn single, double, suite.</summary>
    [Fact]
    public void RoomHelpers_FirstRooms_FollowTheLayout()
    {
        // Assert
        Assert.Equal(["101", "102", "120", "201"], new[] { 1, 2, 20, 21 }.Select(MarketSeedPlanner.RoomNumberText));
        Assert.Equal([RoomSize.Single, RoomSize.Double, RoomSize.Suite, RoomSize.Single], new[] { 1, 2, 3, 4 }.Select(MarketSeedPlanner.SizeOf));
        Assert.Equal([1, 2, 4], new[] { RoomSize.Single, RoomSize.Double, RoomSize.Suite }.Select(MarketSeedPlanner.CapacityOf));
        Assert.Equal([1, 1, 2], new[] { 1, 20, 21 }.Select(MarketSeedPlanner.FloorOf));
    }

    #endregion

    #region Helpers

    private static int PeakOccupancy(IEnumerable<MarketSeedRow> rows)
    {
        var events = new SortedDictionary<DateOnly, int>();
        foreach (var row in rows)
        {
            events[row.Arrival] = events.GetValueOrDefault(row.Arrival) + 1;
            events[row.Departure] = events.GetValueOrDefault(row.Departure) - 1;
        }

        var current = 0;
        var peak = 0;
        foreach (var change in events.Values)
        {
            current += change;
            peak = Math.Max(peak, current);
        }

        return peak;
    }

    internal static MarketSeedRow Row(int number, int arrivalOffset, int nights, bool canceled = false, string country = "Portugal", string segment = "Direct", string roomType = "C") =>
        new(number, canceled, Day.AddDays(arrivalOffset - 30), Day.AddDays(arrivalOffset), nights, 2, 0, 0, "BB", country, segment, false, 0, roomType, 0, "No Deposit", "Contract", 0, 0);

    #endregion
}
