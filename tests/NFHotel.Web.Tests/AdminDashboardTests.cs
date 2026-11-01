using Bunit;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Rooms;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;
using NFHotel.Web.Features.Admin.Dashboard;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>The dashboard of the back office and its view model (MIL-054 task 7).</summary>
public sealed class AdminDashboardTests : IAsyncDisposable
{
    private readonly IBookingService _bookings = Substitute.For<IBookingService>();
    private readonly IRoomService _rooms = Substitute.For<IRoomService>();
    private readonly PageHost _host = new();

    public AdminDashboardTests()
    {
        _bookings.GetOverviewAsync(Arg.Any<BookingOverviewQuery>(), Arg.Any<CancellationToken>()).Returns(List<BookingListItemDto>());
        _rooms.GetAllAsync(Arg.Any<CancellationToken>()).Returns(List<RoomListItemDto>());
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private AdminDashboardViewModel Build() =>
        new(new PassThroughRunner<IBookingService>(_bookings), new PassThroughRunner<IRoomService>(_rooms), new FakeClock { Today = Today });

    private IRenderedComponent<AdminDashboard> Render()
    {
        _host.With(Build()).SignedInWith();
        return _host.Render<AdminDashboard>();
    }

    /// <summary>Positive: today's arrivals, departures and rooms to clean are listed with their badges.</summary>
    [Fact]
    public void Render_BusyDay_ListsEveryTable()
    {
        var departing = BookingItem(2, BookingStatus.CheckedIn) with { StartDate = Today.AddDays(-2), EndDate = Today, EffectiveEndDate = Today };
        _bookings.GetOverviewAsync(Arg.Any<BookingOverviewQuery>(), Arg.Any<CancellationToken>()).Returns(List(BookingItem(1), departing));
        _rooms.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(List(Room(1), Room(2, RoomStatus.Available, HousekeepingStatus.DailyCleaningDue)));

        var page = Render();

        Assert.Contains("Bookings checking in on", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Bookings freeing up their room on", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Rooms where cleaning can be started", page.Markup, StringComparison.Ordinal);
        Assert.Contains("B-0001", page.Markup, StringComparison.Ordinal);
        Assert.Contains("B-0002", page.Markup, StringComparison.Ordinal);
        Assert.Contains("102", page.Markup, StringComparison.Ordinal);
    }

    /// <summary>Edge: a quiet day shows the three empty states, and Refresh loads again.</summary>
    [Fact]
    public void Render_QuietDay_ShowsEmptyStatesAndRefreshes()
    {
        var page = Render();

        Assert.Contains("No arrivals today", page.Markup, StringComparison.Ordinal);
        Assert.Contains("No departures today", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Every room is clean", page.Markup, StringComparison.Ordinal);

        page.Button("Refresh").Click();

        _bookings.Received(2).GetOverviewAsync(Arg.Any<BookingOverviewQuery>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Error handling: both refusals are shown, and the lists stay empty; the day is not called quiet.</summary>
    [Fact]
    public async Task RefreshAsync_BothRefused_ShowsBothErrors()
    {
        _bookings.GetOverviewAsync(Arg.Any<BookingOverviewQuery>(), Arg.Any<CancellationToken>())
            .Returns(Fail<IReadOnlyList<BookingListItemDto>>(ErrorCodes.Booking.NotFound));
        _rooms.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Fail<IReadOnlyList<RoomListItemDto>>(ErrorCodes.Room.NotFound));
        var viewModel = Build();

        await viewModel.InitializeAsync(Xunit.TestContext.Current.CancellationToken);

        Assert.Equal([Text(ErrorCodes.Booking.NotFound), Text(ErrorCodes.Room.NotFound)], viewModel.Errors);
        Assert.Empty(viewModel.Arrivals);
        Assert.Empty(viewModel.RoomsAwaitingCleaning);
        Assert.False(viewModel.IsQuietDay);
    }

    /// <summary>Concurrency: a refresh while one is running is ignored, so the use cases are called once.</summary>
    [Fact]
    public async Task RefreshAsync_WhileBusy_IsIgnored()
    {
        var gate = new TaskCompletionSource<Result<IReadOnlyList<BookingListItemDto>>>();
        _bookings.GetOverviewAsync(Arg.Any<BookingOverviewQuery>(), Arg.Any<CancellationToken>()).Returns(gate.Task);
        var viewModel = Build();

        var first = viewModel.RefreshAsync(Xunit.TestContext.Current.CancellationToken);
        await viewModel.RefreshAsync(Xunit.TestContext.Current.CancellationToken);
        gate.SetResult(Result<IReadOnlyList<BookingListItemDto>>.Success([]));
        await first;

        await _bookings.Received(1).GetOverviewAsync(Arg.Any<BookingOverviewQuery>(), Arg.Any<CancellationToken>());
        Assert.True(viewModel.IsQuietDay);
    }
}
