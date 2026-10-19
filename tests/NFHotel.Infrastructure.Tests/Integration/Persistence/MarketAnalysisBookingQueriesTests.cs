using Microsoft.EntityFrameworkCore;

using NFHotel.Infrastructure.Repositories;
using NFHotel.Infrastructure.Seeding;
using NFHotel.Infrastructure.Tests.Seeding;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>The bookings that are sent to the markets API are exactly the rows of the database (MIL-026).</summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class MarketAnalysisBookingQueriesTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public MarketAnalysisBookingQueriesTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Reading

    /// <summary>Positive: every booking is read once, in id order, with its fields; the counts match a SQL count.</summary>
    [Fact]
    public async Task GetBookingsAsync_SeededBookings_MatchTheRowsOfTheDatabase()
    {
        // Arrange
        await Seed(
            MarketSeedPlannerTests.Row(1, 0, 3),
            MarketSeedPlannerTests.Row(2, 1, 2, country: "Spain"),
            MarketSeedPlannerTests.Row(3, 3, 2, canceled: true, segment: "Complementary"));
        await using var context = _fixture.CreateContext();

        // Act
        var bookings = await new MarketAnalysisBookingQueries(context).GetBookingsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([1, 2, 3], bookings.Select(booking => booking.BookingId));
        Assert.Equal(await context.Bookings.CountAsync(TestContext.Current.CancellationToken), bookings.Count);
        Assert.Equal(await context.Bookings.CountAsync(booking => booking.Status == NFHotel.Domain.ValueObjects.BookingStatus.Cancelled, TestContext.Current.CancellationToken), bookings.Count(booking => booking.IsCanceled));
        Assert.Equal(await context.BookingMarketDetails.CountAsync(detail => detail.PricePerNight == 0m, TestContext.Current.CancellationToken), bookings.Count(booking => booking.PricePerNight == 0m));

        var first = bookings[0];
        Assert.Equal(("NF Hotel", false, 30, new DateOnly(2019, 1, 30), new DateOnly(2019, 3, 1), "Portugal", 94.00m), (first.Hotel, first.IsCanceled, first.LeadTime, first.BookingDate, first.ArrivalDate, first.Country, first.PricePerNight));
        Assert.Equal(("BB", "Direct", "C", "No Deposit", 2, false), (first.Meal, first.MarketSegment, first.AssignedRoomType, first.DepositType, first.Adults, first.IsRepeatedGuest));
        Assert.Equal("Spain", bookings[1].Country);
        Assert.Equal(0m, bookings[2].PricePerNight);
    }

    /// <summary>Positive: the nights are split by the calendar: a Friday to Monday stay is 2 weekend nights and 1 week night.</summary>
    [Fact]
    public async Task GetBookingsAsync_FridayToMonday_SplitsTheNights()
    {
        // Arrange
        await Seed(MarketSeedPlannerTests.Row(1, 0, 3));
        await using var context = _fixture.CreateContext();

        // Act
        var booking = Assert.Single(await new MarketAnalysisBookingQueries(context).GetBookingsAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal(DayOfWeek.Friday, new DateOnly(2019, 3, 1).DayOfWeek);
        Assert.Equal((2, 1), (booking.StaysInWeekendNights, booking.StaysInWeekNights));
    }

    /// <summary>Boundary: a booking made before the market details existed is read with those fields empty and is not left out.</summary>
    [Fact]
    public async Task GetBookingsAsync_BookingWithoutDetails_IsReadWithEmptyFields()
    {
        // Arrange
        await Seed(MarketSeedPlannerTests.Row(1, 0, 3));
        await using var context = _fixture.CreateContext();
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO booking (start_date, end_date, status, room_id, guest_id) VALUES ('2030-01-01', '2030-01-03', 0, 1, 1)",
            TestContext.Current.CancellationToken);

        // Act
        var bookings = await new MarketAnalysisBookingQueries(context).GetBookingsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, bookings.Count);
        var bare = bookings[1];
        Assert.Equal((null, null, null, null, null), (bare.LeadTime, bare.BookingDate, bare.PricePerNight, bare.Adults, bare.Meal));
        Assert.Equal(("Portugal", false, new DateOnly(2030, 1, 1)), (bare.Country, bare.IsCanceled, bare.ArrivalDate));
    }

    /// <summary>Boundary: a hotel with no booking gives an empty list.</summary>
    [Fact]
    public async Task GetBookingsAsync_NoBookings_ReturnsAnEmptyList()
    {
        // Arrange
        await using var context = _fixture.CreateContext();

        // Act
        var bookings = await new MarketAnalysisBookingQueries(context).GetBookingsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(bookings);
    }

    /// <summary>Positive: the real file of 40,060 bookings is read in full, and the cancellations match the file.</summary>
    [Fact]
    public async Task GetBookingsAsync_TheRealFile_ReadsEveryBooking()
    {
        // Arrange
        var path = FindFile();
        if (path is null)
        {
            Assert.Skip("nf_hotel_bookings.csv is not in this checkout.");
        }

        using var reader = new StreamReader(path!);
        var rows = MarketSeedCsv.Parse(reader);
        await using var seedContext = _fixture.CreateContext();
        await new MarketDataSeeder(seedContext).SeedAsync(MarketSeedPlanner.Plan(rows), replace: false, TestContext.Current.CancellationToken);
        await using var context = _fixture.CreateContext();

        // Act
        var bookings = await new MarketAnalysisBookingQueries(context).GetBookingsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(rows.Count, bookings.Count);
        Assert.Equal(rows.Count(row => row.IsCanceled), bookings.Count(booking => booking.IsCanceled));
        Assert.Equal(rows.Sum(row => row.Nights), bookings.Sum(booking => booking.StaysInWeekendNights + booking.StaysInWeekNights));
        Assert.Equal(rows.Sum(row => row.Arrival.DayNumber - row.BookedAt.DayNumber), bookings.Sum(booking => (long)booking.LeadTime!.Value));
    }

    #endregion

    #region Helpers

    private async Task Seed(params MarketSeedRow[] rows)
    {
        await using var context = _fixture.CreateContext();
        await new MarketDataSeeder(context).SeedAsync(MarketSeedPlanner.Plan(rows), replace: false, TestContext.Current.CancellationToken);
    }

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
