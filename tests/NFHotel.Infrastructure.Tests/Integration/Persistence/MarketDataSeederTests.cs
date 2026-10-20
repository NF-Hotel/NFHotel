using Microsoft.EntityFrameworkCore;

using NFHotel.Infrastructure.Seeding;
using NFHotel.Infrastructure.Tests.Seeding;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>The seed of the fake bookings against a real PostgreSQL, with the overlap constraint in place (MIL-026).</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class MarketDataSeederTests : IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 10, 20);

    private readonly PostgreSqlFixture _fixture;

    public MarketDataSeederTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Seeding

    /// <summary>Positive: rooms, guests, bookings and details are written with the keys of the file, and the overlap constraint accepts them.</summary>
    [Fact]
    public async Task SeedAsync_EmptyDatabase_WritesEverything()
    {
        // Arrange
        var plan = SmallPlan();
        await using var context = _fixture.CreateContext();

        // Act
        await new MarketDataSeeder(context).SeedAsync(plan, Today, replace: false, TestContext.Current.CancellationToken);

        // Assert
        await using var check = _fixture.CreateContext();
        Assert.Equal(plan.RoomCount, await check.Rooms.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(plan.GuestCountries.Count, await check.Guests.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(4, await check.Bookings.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(4, await check.BookingMarketDetails.CountAsync(TestContext.Current.CancellationToken));

        var booking = await check.Bookings.SingleAsync(candidate => candidate.Id == 1, TestContext.Current.CancellationToken);
        Assert.Equal((new DateOnly(2019, 3, 1), new DateOnly(2019, 3, 4)), (booking.StartDate, booking.EndDate));
        Assert.Equal(NFHotel.Domain.ValueObjects.BookingStatus.CheckedOut, booking.Status);
        Assert.Equal(new DateOnly(2019, 3, 4), booking.CheckOutDate);

        var cancelled = await check.Bookings.SingleAsync(candidate => candidate.Id == 4, TestContext.Current.CancellationToken);
        Assert.Equal(NFHotel.Domain.ValueObjects.BookingStatus.Cancelled, cancelled.Status);
        Assert.Null(cancelled.CheckInTime);

        var detail = await check.BookingMarketDetails.SingleAsync(candidate => candidate.BookingId == 1, TestContext.Current.CancellationToken);
        Assert.Equal((new DateOnly(2019, 1, 30), 94.00m, "BB", "Direct", "C", 2), (detail.BookedAt, detail.PricePerNight, detail.Meal, detail.MarketSegment, detail.RoomType, detail.Adults));
    }

    /// <summary>State: after the seed the next new row of each table gets a key above the loaded ones.</summary>
    [Fact]
    public async Task SeedAsync_AfterTheSeed_NewRowsGetFreshKeys()
    {
        // Arrange
        await using (var context = _fixture.CreateContext())
        {
            await new MarketDataSeeder(context).SeedAsync(SmallPlan(), Today, replace: false, TestContext.Current.CancellationToken);
        }

        // Act
        await using var add = _fixture.CreateContext();
        await add.Database.ExecuteSqlRawAsync(
            "INSERT INTO room (room_number, floor, size, capacity, status, housekeeping_status) VALUES ('999', 9, 0, 1, 0, 0)",
            TestContext.Current.CancellationToken);

        // Assert
        var rooms = await add.Rooms.CountAsync(TestContext.Current.CancellationToken);
        Assert.True(rooms > 1);
        Assert.Equal(rooms, await add.Rooms.Select(room => room.Id).Distinct().CountAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>Negative: a database that already has data is refused and left as it was.</summary>
    [Fact]
    public async Task SeedAsync_DatabaseHasData_RefusesWithoutReplace()
    {
        // Arrange
        await using (var context = _fixture.CreateContext())
        {
            await new MarketDataSeeder(context).SeedAsync(SmallPlan(), Today, replace: false, TestContext.Current.CancellationToken);
        }

        await using var again = _fixture.CreateContext();

        // Act
        var seed = () => new MarketDataSeeder(again).SeedAsync(SmallPlan(), Today, replace: false, TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(seed);
        await using var check = _fixture.CreateContext();
        Assert.Equal(4, await check.Bookings.CountAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>Positive: with replace the old data goes and the same plan gives the same data again.</summary>
    [Fact]
    public async Task SeedAsync_Replace_GivesTheSameDataAgain()
    {
        // Arrange
        await using (var context = _fixture.CreateContext())
        {
            await new MarketDataSeeder(context).SeedAsync(SmallPlan(), Today, replace: false, TestContext.Current.CancellationToken);
        }

        // Act
        await using (var again = _fixture.CreateContext())
        {
            await new MarketDataSeeder(again).SeedAsync(SmallPlan(), Today, replace: true, TestContext.Current.CancellationToken);
        }

        // Assert
        await using var check = _fixture.CreateContext();
        Assert.Equal(4, await check.Bookings.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(4, await check.BookingMarketDetails.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal([1, 2, 3, 4], await check.Bookings.OrderBy(booking => booking.Id).Select(booking => booking.Id).ToListAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>Negative: a plan that breaks the overlap rule is refused by the database and nothing is written.</summary>
    [Fact]
    public async Task SeedAsync_OverlapInOneRoom_IsRefusedAndRollsBack()
    {
        // Arrange
        var rows = new[] { MarketSeedPlannerTests.Row(1, 0, 3), MarketSeedPlannerTests.Row(2, 1, 3) };
        var plan = MarketSeedPlanner.Plan(rows);
        var broken = plan with { Bookings = [.. plan.Bookings.Select(placement => placement with { Room = 1 })] };
        await using var context = _fixture.CreateContext();

        // Act
        var seed = () => new MarketDataSeeder(context).SeedAsync(broken, Today, replace: false, TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAnyAsync<Exception>(seed);
        await using var check = _fixture.CreateContext();
        Assert.Equal(0, await check.Bookings.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, await check.Rooms.CountAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>Positive: the real file of the repository loads, and its bookings and its cancellations are all there.</summary>
    [Fact]
    public async Task SeedAsync_TheRealFile_LoadsEveryRow()
    {
        // Arrange
        var path = FindFile();
        if (path is null)
        {
            Assert.Skip("nf_hotel_bookings.csv is not in this checkout.");
        }

        using var reader = new StreamReader(path!);
        var rows = MarketSeedCsv.Parse(reader);
        var plan = MarketSeedPlanner.Plan(rows);
        await using var context = _fixture.CreateContext();

        // Act
        await new MarketDataSeeder(context).SeedAsync(plan, Today, replace: false, TestContext.Current.CancellationToken);

        // Assert
        await using var check = _fixture.CreateContext();
        Assert.Equal(rows.Count, await check.Bookings.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(rows.Count(row => row.IsCanceled), await check.Bookings.CountAsync(booking => booking.Status == NFHotel.Domain.ValueObjects.BookingStatus.Cancelled, TestContext.Current.CancellationToken));
        Assert.Equal(rows.Count, await check.BookingMarketDetails.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(plan.RoomCount, await check.Rooms.CountAsync(TestContext.Current.CancellationToken));
    }



    /// <summary>Positive: the status follows today: cancelled stays cancelled, left is checked out, here is checked in, later is confirmed.</summary>
    [Fact]
    public async Task SeedAsync_BookingsAroundToday_GetTheirStatusFromTheDate()
    {
        // Arrange: today is 2026-10-20
        var plan = MarketSeedPlanner.Plan(
        [
            Row(1, new DateOnly(2026, 10, 1), 3),
            Row(2, new DateOnly(2026, 10, 19), 3),
            Row(3, new DateOnly(2026, 10, 20), 2),
            Row(4, new DateOnly(2026, 12, 24), 3),
            Row(5, new DateOnly(2026, 10, 1), 3, canceled: true),
        ]);
        await using var context = _fixture.CreateContext();

        // Act
        await new MarketDataSeeder(context).SeedAsync(plan, Today, replace: false, TestContext.Current.CancellationToken);

        // Assert
        await using var check = _fixture.CreateContext();
        var bookings = await check.Bookings.OrderBy(booking => booking.Id).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(
            [NFHotel.Domain.ValueObjects.BookingStatus.CheckedOut, NFHotel.Domain.ValueObjects.BookingStatus.CheckedIn, NFHotel.Domain.ValueObjects.BookingStatus.CheckedIn, NFHotel.Domain.ValueObjects.BookingStatus.Confirmed, NFHotel.Domain.ValueObjects.BookingStatus.Cancelled],
            bookings.Select(booking => booking.Status));
        Assert.Equal(new DateOnly(2026, 10, 4), bookings[0].CheckOutDate);
        Assert.NotNull(bookings[1].CheckInTime);
        Assert.Null(bookings[1].CheckOutTime);
        Assert.Null(bookings[3].CheckInTime);
    }

    /// <summary>Positive: the real file, repeated through 2026-12-31, loads with the overlap constraint on and has bookings up to the date.</summary>
    [Fact]
    public async Task SeedAsync_TheRealFileThrough2026_LoadsEveryRowUpToTheDate()
    {
        // Arrange
        var path = FindFile();
        if (path is null)
        {
            Assert.Skip("nf_hotel_bookings.csv is not in this checkout.");
        }

        using var reader = new StreamReader(path!);
        var rows = MarketSeedExpander.Expand(MarketSeedCsv.Parse(reader), new DateOnly(2026, 12, 31));
        await using var context = _fixture.CreateContext();

        // Act
        await new MarketDataSeeder(context).SeedAsync(MarketSeedPlanner.Plan(rows), Today, replace: false, TestContext.Current.CancellationToken);

        // Assert
        await using var check = _fixture.CreateContext();
        Assert.Equal(rows.Count, await check.Bookings.CountAsync(TestContext.Current.CancellationToken));
        Assert.Equal(new DateOnly(2018, 7, 1), await check.Bookings.MinAsync(booking => booking.StartDate, TestContext.Current.CancellationToken));
        Assert.True(await check.Bookings.MaxAsync(booking => booking.StartDate, TestContext.Current.CancellationToken) > new DateOnly(2026, 12, 1));
        Assert.True(await check.Bookings.AnyAsync(booking => booking.Status == NFHotel.Domain.ValueObjects.BookingStatus.Confirmed, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Helpers

    private static MarketSeedRow Row(int number, DateOnly arrival, int nights, bool canceled = false) =>
        MarketSeedPlannerTests.Row(number, 0, nights, canceled) with { Arrival = arrival, BookedAt = arrival.AddDays(-30) };

    private static MarketSeedPlan SmallPlan() =>
        MarketSeedPlanner.Plan(
        [
            MarketSeedPlannerTests.Row(1, 0, 3),
            MarketSeedPlannerTests.Row(2, 1, 2, country: "Spain"),
            MarketSeedPlannerTests.Row(3, 3, 2),
            MarketSeedPlannerTests.Row(4, 0, 3, canceled: true),
        ]);

    private static string? FindFile()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "NFHotel.DataAnalysis", "data", "nf_hotel_bookings.csv");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    #endregion
}
