using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;
using NFHotel.Web.Features.Admin.Bookings;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>The presentation logic of the availability calendar on the booking page.</summary>
public sealed class BookingCalendarViewModelTests
{
    #region Setup

    private readonly IBookingService _bookings = Substitute.For<IBookingService>();
    private readonly FakeClock _clock = new() { Today = Today };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    public BookingCalendarViewModelTests() =>
        _bookings.GetAvailabilityAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(List(Night(Today, 0), Night(Today.AddDays(1), 2)));

    private BookingCalendarViewModel Create() => new(new PassThroughRunner<IBookingService>(_bookings), _clock);

    private static NightAvailabilityDto Night(DateOnly date, int freeDoubles) =>
        new(date,
        [
            new RoomTypeAvailabilityDto(RoomSize.Single, 1, 1, ["101"]),
            new RoomTypeAvailabilityDto(RoomSize.Double, freeDoubles, 2, freeDoubles == 0 ? [] : ["102", "103"]),
            new RoomTypeAvailabilityDto(RoomSize.Suite, 1, 1, ["105"]),
        ]);

    #endregion

    #region Constructor

    /// <summary>Negative: each dependency is required.</summary>
    [Fact]
    public void Constructor_MissingDependency_Throws()
    {
        PassThroughRunner<IBookingService> bookings = new(_bookings);

        Assert.Throws<ArgumentNullException>(() => new BookingCalendarViewModel(null!, _clock));
        Assert.Throws<ArgumentNullException>(() => new BookingCalendarViewModel(bookings, null!));
    }

    /// <summary>Positive: a new calendar shows the month of today, with every size and nothing fetched yet.</summary>
    [Fact]
    public void Constructor_NewCalendar_ShowsTheMonthOfToday()
    {
        BookingCalendarViewModel viewModel = Create();

        Assert.Equal(new DateOnly(2026, 3, 1), viewModel.Month);
        Assert.Equal("March 2026", viewModel.MonthText);
        Assert.True(viewModel.IsThisMonth);
        Assert.Null(viewModel.SizeFilter);
        Assert.False(viewModel.IsLoaded);
    }

    #endregion

    #region Loading

    /// <summary>Positive: the calendar asks for the whole grid, the weeks around the month included, and lays it out.</summary>
    [Fact]
    public async Task InitializeAsync_Month_FetchesTheWholeGrid()
    {
        BookingCalendarViewModel viewModel = Create();

        await viewModel.InitializeAsync(Token);

        await _bookings.Received(1).GetAvailabilityAsync(new DateOnly(2026, 2, 23), new DateOnly(2026, 4, 6), Arg.Any<CancellationToken>());
        Assert.True(viewModel.IsLoaded);
        Assert.False(viewModel.IsBusy);
        Assert.Empty(viewModel.Errors);
        Assert.Equal(42, viewModel.Days.Count);
        Assert.Equal(0, viewModel.Days.Single(day => day.Date == Today).Slots[1].Free);
        Assert.Equal("1 night ahead this month with a size sold out", viewModel.SoldOutText);
    }

    /// <summary>Negative: an error from the service is shown, and the grid has no figures.</summary>
    [Fact]
    public async Task LoadAsync_Failure_ShowsTheErrorWithoutFigures()
    {
        _bookings.GetAvailabilityAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Fail<IReadOnlyList<NightAvailabilityDto>>(ErrorCodes.Booking.AvailabilityPeriodTooLong));
        BookingCalendarViewModel viewModel = Create();

        await viewModel.LoadAsync(Token);

        Assert.Equal(["The calendar can show at most two months at a time."], viewModel.Errors);
        Assert.All(viewModel.Days, day => Assert.False(day.HasFigures));
        Assert.True(viewModel.IsLoaded);
    }

    #endregion

    #region Months

    /// <summary>Positive: the arrows move a month at a time across the year, and Today goes back.</summary>
    [Fact]
    public async Task MonthNavigation_ArrowsAndToday_FetchTheMonthShown()
    {
        BookingCalendarViewModel viewModel = Create();

        await viewModel.PreviousMonthAsync(Token);
        await viewModel.PreviousMonthAsync(Token);
        await viewModel.PreviousMonthAsync(Token);
        Assert.Equal(new DateOnly(2025, 12, 1), viewModel.Month);
        Assert.False(viewModel.IsThisMonth);

        await viewModel.NextMonthAsync(Token);
        Assert.Equal(new DateOnly(2026, 1, 1), viewModel.Month);
        // January is fetched on the way back and again on the way forward.
        await _bookings.Received(2).GetAvailabilityAsync(new DateOnly(2025, 12, 29), new DateOnly(2026, 2, 2), Arg.Any<CancellationToken>());

        await viewModel.ThisMonthAsync(Token);
        Assert.True(viewModel.IsThisMonth);
        Assert.Equal("March 2026", viewModel.MonthText);
    }

    #endregion

    #region Filter

    /// <summary>Positive: the size filter narrows the figures already fetched, without a new call.</summary>
    [Fact]
    public async Task FilterBySize_OneSize_NarrowsWithoutFetching()
    {
        BookingCalendarViewModel viewModel = Create();
        await viewModel.InitializeAsync(Token);

        viewModel.FilterBySize(RoomSize.Suite);

        await _bookings.Received(1).GetAvailabilityAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
        Assert.Equal(RoomSize.Suite, viewModel.SizeFilter);
        Assert.Equal([RoomSize.Suite], viewModel.Days.Single(day => day.Date == Today).Slots.Select(slot => slot.Size).ToArray());
        Assert.Equal("0 nights ahead this month with suites sold out", viewModel.SoldOutText);

        viewModel.FilterBySize(null);

        Assert.Equal(3, viewModel.Days.Single(day => day.Date == Today).Slots.Count);
    }

    /// <summary>Equivalence: a booking can start today or later, never on a past day (BR-44).</summary>
    [Fact]
    public async Task CanPick_PastAndToday_FollowTheClock()
    {
        BookingCalendarViewModel viewModel = Create();
        await viewModel.InitializeAsync(Token);

        Assert.True(BookingCalendarViewModel.CanPick(viewModel.Days.Single(day => day.Date == Today)));
        Assert.False(BookingCalendarViewModel.CanPick(viewModel.Days.Single(day => day.Date == Today.AddDays(-1))));
    }

    #endregion
}
