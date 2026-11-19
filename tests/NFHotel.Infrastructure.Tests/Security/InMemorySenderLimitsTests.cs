using NFHotel.Application.Ports;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;
using NFHotel.TestSupport;

namespace NFHotel.Infrastructure.Tests.Security;

/// <summary>
/// The limits per caller address of the booking flow, in memory (MIL-087 criteria 3 and 5, ADR-0051 decisions 2 and 5):
/// 60 searches a minute, 5 booking requests an hour and 20 a day, each window fixed from the first request that it counts,
/// the check and the count one step, and three stores that never hold more than their bound.
/// </summary>
public sealed class InMemorySenderLimitsTests
{
    private const string Address = "203.0.113.10";

    #region The searches

    /// <summary>Boundary: sixty searches of one address are counted, the 61st is refused with the name of its limit.</summary>
    [Fact]
    public void TryCountSearch_SixtyFirstInAMinute_IsRefusedByTheLimitOfTheSearch()
    {
        // Arrange
        InMemorySenderLimits limits = Build(new FakeClock());

        // Act
        SenderLimitDecision[] decisions = [.. Enumerable.Range(0, 61).Select(_ => limits.TryCountSearch(Address))];

        // Assert
        Assert.All(decisions[..60], decision => Assert.True(decision.IsAllowed));
        Assert.False(decisions[60].IsAllowed);
        Assert.Equal(LimitNames.Search, decisions[60].Limit);
    }

    /// <summary>
    /// Boundary: the window is fixed from the first search. A second before its end the address is still refused, with
    /// the second that is left as the time to wait; at the end it is counted in a new window.
    /// </summary>
    [Fact]
    public void TryCountSearch_RefusedThenAtTheEndOfTheMinute_CountsInANewWindow()
    {
        // Arrange
        FakeClock clock = new();
        InMemorySenderLimits limits = Build(clock);
        CountSearches(limits, Address, 60);

        // Act
        clock.TimeOfDay += TimeSpan.FromSeconds(59);
        SenderLimitDecision beforeTheEnd = limits.TryCountSearch(Address);
        clock.TimeOfDay += TimeSpan.FromSeconds(1);
        SenderLimitDecision atTheEnd = limits.TryCountSearch(Address);

        // Assert
        Assert.False(beforeTheEnd.IsAllowed);
        Assert.Equal(TimeSpan.FromSeconds(1), beforeTheEnd.RetryAfter);
        Assert.True(atTheEnd.IsAllowed);
    }

    #endregion

    #region The booking requests

    /// <summary>
    /// Boundary: five booking requests of one address are counted, the sixth is refused by the limit of the hour with
    /// the time that is left of the hour.
    /// </summary>
    [Fact]
    public void TryCountBookingRequest_SixthInAnHour_IsRefusedByTheLimitOfTheHour()
    {
        // Arrange
        FakeClock clock = new();
        InMemorySenderLimits limits = Build(clock);
        SenderLimitDecision[] counted = [.. Enumerable.Range(0, 5).Select(_ => limits.TryCountBookingRequest(Address))];
        clock.TimeOfDay += TimeSpan.FromMinutes(10);

        // Act
        SenderLimitDecision refused = limits.TryCountBookingRequest(Address);

        // Assert
        Assert.All(counted, decision => Assert.True(decision.IsAllowed));
        Assert.False(refused.IsAllowed);
        Assert.Equal(LimitNames.BookingHour, refused.Limit);
        Assert.Equal(TimeSpan.FromMinutes(50), refused.RetryAfter);
    }

    /// <summary>
    /// Boundary: five requests in each of four hours make twenty; the 21st is refused by the limit of the day with the
    /// twenty hours that are left of it, and a day after the first request the count starts again.
    /// </summary>
    [Fact]
    public void TryCountBookingRequest_TwentyFirstInADay_IsRefusedByTheLimitOfTheDay()
    {
        // Arrange
        FakeClock clock = new();
        InMemorySenderLimits limits = Build(clock);
        for (int hour = 0; hour < 4; hour++)
        {
            CountBookingRequests(limits, Address, 5);
            clock.TimeOfDay += TimeSpan.FromHours(1);
        }

        // Act
        SenderLimitDecision refused = limits.TryCountBookingRequest(Address);
        clock.TimeOfDay += TimeSpan.FromHours(20);
        SenderLimitDecision nextDay = limits.TryCountBookingRequest(Address);

        // Assert
        Assert.False(refused.IsAllowed);
        Assert.Equal(LimitNames.BookingDay, refused.Limit);
        Assert.Equal(TimeSpan.FromHours(20), refused.RetryAfter);
        Assert.True(nextDay.IsAllowed);
    }

    /// <summary>
    /// Equivalence (ADR-0051 decision 2): a refused request counts in no window. Ten refusals in the first hour do not
    /// bring the day closer to its limit: fifteen more requests in the next three hours are counted.
    /// </summary>
    [Fact]
    public void TryCountBookingRequest_Refused_CountsInNoWindow()
    {
        // Arrange
        FakeClock clock = new();
        InMemorySenderLimits limits = Build(clock);
        CountBookingRequests(limits, Address, 5);
        SenderLimitDecision[] refused = [.. Enumerable.Range(0, 10).Select(_ => limits.TryCountBookingRequest(Address))];

        // Act
        List<SenderLimitDecision> later = [];
        for (int hour = 1; hour <= 3; hour++)
        {
            clock.TimeOfDay += TimeSpan.FromHours(1);
            later.AddRange(Enumerable.Range(0, 5).Select(_ => limits.TryCountBookingRequest(Address)));
        }

        // Assert
        Assert.All(refused, decision => Assert.False(decision.IsAllowed));
        Assert.All(later, decision => Assert.True(decision.IsAllowed));
    }

    /// <summary>
    /// State transition: a request given back leaves room for one more in the hour and in the day, and only one: the
    /// seventh request is refused.
    /// </summary>
    [Fact]
    public void GiveBackBookingRequest_AfterFive_LeavesRoomForOneMore()
    {
        // Arrange
        InMemorySenderLimits limits = Build(new FakeClock());
        CountBookingRequests(limits, Address, 5);

        // Act
        limits.GiveBackBookingRequest(Address);
        SenderLimitDecision sixth = limits.TryCountBookingRequest(Address);
        SenderLimitDecision seventh = limits.TryCountBookingRequest(Address);

        // Assert
        Assert.True(sixth.IsAllowed);
        Assert.False(seventh.IsAllowed);
    }

    /// <summary>Negative: a request given back for an address without a window, or more often than counted, changes nothing.</summary>
    [Fact]
    public void GiveBackBookingRequest_WithoutACount_ChangesNothing()
    {
        // Arrange
        InMemorySenderLimits limits = Build(new FakeClock());

        // Act
        limits.GiveBackBookingRequest(Address);
        limits.GiveBackBookingRequest(Address);
        SenderLimitDecision[] decisions = [.. Enumerable.Range(0, 6).Select(_ => limits.TryCountBookingRequest(Address))];

        // Assert
        Assert.Equal([true, true, true, true, true, false], decisions.Select(decision => decision.IsAllowed));
    }

    #endregion

    #region One address against another

    /// <summary>Equivalence (criterion 5): the requests of one address do not use up the count of another.</summary>
    [Fact]
    public void TryCount_OtherAddress_IsCountedApart()
    {
        // Arrange
        InMemorySenderLimits limits = Build(new FakeClock());
        CountSearches(limits, Address, 60);
        CountBookingRequests(limits, Address, 5);

        // Act
        SenderLimitDecision search = limits.TryCountSearch("203.0.113.11");
        SenderLimitDecision booking = limits.TryCountBookingRequest("203.0.113.11");

        // Assert
        Assert.False(limits.TryCountSearch(Address).IsAllowed);
        Assert.True(search.IsAllowed);
        Assert.True(booking.IsAllowed);
    }

    /// <summary>
    /// Security (ADR-0051 decision 2): requests sent at the same time cannot all pass the check before any of them is
    /// counted: of 1,000 searches and 100 booking requests from one address on many threads, 60 and 5 are counted.
    /// </summary>
    [Fact]
    public void TryCount_ManyAtTheSameTime_CountsExactlyTheLimit()
    {
        // Arrange
        InMemorySenderLimits limits = Build(new FakeClock());
        int searches = 0;
        int bookings = 0;

        // Act
        Parallel.For(0, 1_000, number =>
        {
            if (limits.TryCountSearch(Address).IsAllowed)
            {
                Interlocked.Increment(ref searches);
            }

            if (number % 10 == 0 && limits.TryCountBookingRequest(Address).IsAllowed)
            {
                Interlocked.Increment(ref bookings);
            }
        });

        // Assert
        Assert.Equal(60, searches);
        Assert.Equal(5, bookings);
    }

    #endregion

    #region The bound

    /// <summary>
    /// Security (criterion 5, ADR-0051 decision 5): after 100,000 requests from different addresses each store holds at
    /// most its 10,000 keys, and an address whose searches and requests are used up stays refused.
    /// </summary>
    [Fact]
    public void TryCount_HundredThousandAddresses_StaysWithinTheBound()
    {
        // Arrange
        InMemorySenderLimits limits = Build(new FakeClock());
        CountSearches(limits, Address, 60);
        CountBookingRequests(limits, Address, 5);

        // Act
        for (int number = 0; number < 100_000; number++)
        {
            string other = $"10.{number / 65_536}.{number / 256 % 256}.{number % 256}";
            limits.TryCountSearch(other);
            limits.TryCountBookingRequest(other);
        }

        // Assert
        Assert.InRange(limits.TrackedSearchAddressCount, 1, 10_000);
        Assert.InRange(limits.TrackedBookingHourAddressCount, 1, 10_000);
        Assert.InRange(limits.TrackedBookingDayAddressCount, 1, 10_000);
        Assert.False(limits.TryCountSearch(Address).IsAllowed);
        Assert.False(limits.TryCountBookingRequest(Address).IsAllowed);
    }

    /// <summary>
    /// Security (review RC-428 F2, ADR-0051 decision 5): when every window of the full store of the searches is at its
    /// limit, a new address is still counted and the window that runs out first is dropped, so senders with more
    /// addresses than the store holds cannot make the limit refuse every other address; the store keeps its bound, and
    /// an address at its limit whose window was not dropped stays refused.
    /// </summary>
    [Fact]
    public void TryCountSearch_FullStoreOfAddressesAtTheirLimit_CountsANewAddressAndDropsTheFirstToRunOut()
    {
        // Arrange
        FakeClock clock = new();
        InMemorySenderLimits limits = new(SmallOptions(), clock);
        CountSearches(limits, "203.0.113.1", 2);
        clock.TimeOfDay += TimeSpan.FromSeconds(1);
        CountSearches(limits, "203.0.113.2", 2);
        clock.TimeOfDay += TimeSpan.FromSeconds(1);

        // Act
        SenderLimitDecision newcomer = limits.TryCountSearch("203.0.113.3");

        // Assert: the first address lost its window, so it starts a new one; the second keeps its used-up window.
        Assert.True(newcomer.IsAllowed);
        Assert.Equal(2, limits.TrackedSearchAddressCount);
        Assert.True(limits.TryCountSearch("203.0.113.1").IsAllowed);
        Assert.False(limits.TryCountSearch("203.0.113.2").IsAllowed);
        Assert.Equal(2, limits.TrackedSearchAddressCount);
    }

    /// <summary>
    /// Security (review RC-428 F2, ADR-0051 decision 5): the same for the booking requests, in the store of the hour and
    /// the store of the day: a new address is counted in both, the stores keep their bound, and an address at both
    /// limits stays refused.
    /// </summary>
    [Fact]
    public void TryCountBookingRequest_FullStoresOfAddressesAtTheirLimits_CountANewAddressAndDropTheFirstToRunOut()
    {
        // Arrange: two requests an hour and two a day, so both stores hold two addresses at their limits.
        FakeClock clock = new();
        InMemorySenderLimits limits = new(SmallOptions(), clock);
        CountBookingRequests(limits, "203.0.113.1", 2);
        clock.TimeOfDay += TimeSpan.FromSeconds(1);
        CountBookingRequests(limits, "203.0.113.2", 2);
        clock.TimeOfDay += TimeSpan.FromSeconds(1);

        // Act
        SenderLimitDecision newcomer = limits.TryCountBookingRequest("203.0.113.3");

        // Assert
        Assert.True(newcomer.IsAllowed);
        Assert.Equal(2, limits.TrackedBookingHourAddressCount);
        Assert.Equal(2, limits.TrackedBookingDayAddressCount);
        Assert.False(limits.TryCountBookingRequest("203.0.113.2").IsAllowed);
    }

    #endregion

    #region The settings

    /// <summary>Negative: a number below one is a mistake of the setup, which must not switch a limit off.</summary>
    /// <param name="setting">The setting that is set to zero.</param>
    [Theory]
    [InlineData(nameof(SenderLimitOptions.SearchesPerMinute))]
    [InlineData(nameof(SenderLimitOptions.BookingRequestsPerHour))]
    [InlineData(nameof(SenderLimitOptions.BookingRequestsPerDay))]
    [InlineData(nameof(SenderLimitOptions.MaxTrackedKeys))]
    public void Constructor_SettingBelowOne_Throws(string setting)
    {
        // Arrange
        SenderLimitOptions options = new()
        {
            SearchesPerMinute = setting == nameof(SenderLimitOptions.SearchesPerMinute) ? 0 : 60,
            BookingRequestsPerHour = setting == nameof(SenderLimitOptions.BookingRequestsPerHour) ? 0 : 5,
            BookingRequestsPerDay = setting == nameof(SenderLimitOptions.BookingRequestsPerDay) ? 0 : 20,
            ComingPublicBookingsPerEmail = 3,
            MaxTrackedKeys = setting == nameof(SenderLimitOptions.MaxTrackedKeys) ? 0 : 10_000,
        };

        // Act and assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new InMemorySenderLimits(options, new FakeClock()));
    }

    /// <summary>
    /// Negative (review RC-428 F11): a limit above 100,000 is a mistake of the setup, refused when the limits are made,
    /// before a store sets aside room for every count up to it.
    /// </summary>
    /// <param name="setting">The setting that is set to one more than the cap.</param>
    [Theory]
    [InlineData(nameof(SenderLimitOptions.SearchesPerMinute))]
    [InlineData(nameof(SenderLimitOptions.BookingRequestsPerHour))]
    [InlineData(nameof(SenderLimitOptions.BookingRequestsPerDay))]
    public void Constructor_LimitAboveTheCap_Throws(string setting)
    {
        // Arrange
        SenderLimitOptions options = new()
        {
            SearchesPerMinute = setting == nameof(SenderLimitOptions.SearchesPerMinute) ? 100_001 : 60,
            BookingRequestsPerHour = setting == nameof(SenderLimitOptions.BookingRequestsPerHour) ? 100_001 : 5,
            BookingRequestsPerDay = setting == nameof(SenderLimitOptions.BookingRequestsPerDay) ? 100_001 : 20,
            ComingPublicBookingsPerEmail = 3,
            MaxTrackedKeys = 10_000,
        };

        // Act and assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new InMemorySenderLimits(options, new FakeClock()));
    }

    #endregion

    #region Helpers

    private static SenderLimitOptions Options() => new()
    {
        SearchesPerMinute = 60,
        BookingRequestsPerHour = 5,
        BookingRequestsPerDay = 20,
        ComingPublicBookingsPerEmail = 3,
        MaxTrackedKeys = 10_000,
    };

    private static InMemorySenderLimits Build(FakeClock clock) => new(Options(), clock);

    // Two of each in stores of two addresses, so a test fills a store with a few requests.
    private static SenderLimitOptions SmallOptions() => new()
    {
        SearchesPerMinute = 2,
        BookingRequestsPerHour = 2,
        BookingRequestsPerDay = 2,
        ComingPublicBookingsPerEmail = 3,
        MaxTrackedKeys = 2,
    };

    private static void CountSearches(InMemorySenderLimits limits, string address, int count)
    {
        for (int search = 0; search < count; search++)
        {
            Assert.True(limits.TryCountSearch(address).IsAllowed);
        }
    }

    private static void CountBookingRequests(InMemorySenderLimits limits, string address, int count)
    {
        for (int request = 0; request < count; request++)
        {
            Assert.True(limits.TryCountBookingRequest(address).IsAllowed);
        }
    }

    #endregion
}
