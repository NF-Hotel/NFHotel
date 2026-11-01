using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;
using NFHotel.Web.Features.Admin.Bookings;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>The presentation logic of the booking overview of the back office (MIL-054 task 2).</summary>
public sealed class BookingListViewModelTests
{
    #region Setup

    private readonly IBookingService _bookings = Substitute.For<IBookingService>();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private BookingListViewModel Create() =>
        new(new PassThroughRunner<IBookingService>(_bookings), new FakeClock { Today = Today });

    private void OverviewIs(params BookingListItemDto[] bookings) =>
        _bookings.GetOverviewAsync(Arg.Any<BookingOverviewQuery>(), Arg.Any<CancellationToken>()).Returns(List(bookings));

    #endregion

    #region Constructor

    /// <summary>Negative: each dependency is required.</summary>
    [Fact]
    public void Constructor_MissingDependency_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new BookingListViewModel(null!, new FakeClock()));
        Assert.Throws<ArgumentNullException>(() => new BookingListViewModel(new PassThroughRunner<IBookingService>(_bookings), null!));
    }

    #endregion

    #region InitializeAsync

    /// <summary>Positive: the first load asks for the next 14 days, sorted by start date, without cancelled bookings.</summary>
    [Fact]
    public async Task InitializeAsync_FirstLoad_AsksForTheNextTwoWeeks()
    {
        OverviewIs(BookingItem(1), BookingItem(2));
        var viewModel = Create();

        await viewModel.InitializeAsync(Token);

        Assert.True(viewModel.IsLoaded);
        Assert.Equal(2, viewModel.Bookings.Count);
        Assert.False(viewModel.IsEmpty);
        await _bookings.Received(1).GetOverviewAsync(
            new BookingOverviewQuery(Today, Today.AddDays(14), null, BookingSortField.StartDate, SortDirection.Ascending, false),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region SearchAsync

    /// <summary>Edge: blank search text is sent as no search; the other filters are sent as set.</summary>
    [Fact]
    public async Task SearchAsync_BlankTextAndCancelledShown_SendsTheFilters()
    {
        OverviewIs();
        var viewModel = Create();
        viewModel.SearchText = "  ";
        viewModel.IncludeCancelled = true;

        await viewModel.SearchAsync(Token);

        Assert.True(viewModel.IsEmpty);
        Assert.False(viewModel.IsHiddenByStatusFilter);
        await _bookings.Received(1).GetOverviewAsync(
            Arg.Is<BookingOverviewQuery>(query => query.SearchText == null && query.IncludeCancelled),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Error handling: a refused load shows the text and empties the list.</summary>
    [Fact]
    public async Task SearchAsync_Refused_ShowsTheErrorAndEmptiesTheList()
    {
        _bookings.GetOverviewAsync(Arg.Any<BookingOverviewQuery>(), Arg.Any<CancellationToken>())
            .Returns(Fail<IReadOnlyList<BookingListItemDto>>(ErrorCodes.Booking.NotFound));
        var viewModel = Create();

        await viewModel.SearchAsync(Token);

        Assert.Equal([Text(ErrorCodes.Booking.NotFound)], viewModel.Errors);
        Assert.Empty(viewModel.Bookings);
        Assert.False(viewModel.IsEmpty);
    }

    #endregion

    #region FilterByStatus

    /// <summary>Equivalence: the status filter is applied on the loaded rows, and says when it hides every row.</summary>
    [Fact]
    public async Task FilterByStatus_StatusWithoutRows_IsHiddenByStatusFilter()
    {
        OverviewIs(BookingItem(1, BookingStatus.Pending), BookingItem(2, BookingStatus.Confirmed));
        var viewModel = Create();
        await viewModel.SearchAsync(Token);

        viewModel.FilterByStatus(BookingStatus.Confirmed);
        Assert.Equal([2], viewModel.Bookings.Select(booking => booking.BookingId));

        viewModel.FilterByStatus(BookingStatus.CheckedIn);
        Assert.True(viewModel.IsHiddenByStatusFilter);

        viewModel.FilterByStatus(null);
        Assert.Equal(2, viewModel.Bookings.Count);
    }

    #endregion

    #region ClearFilterAsync

    /// <summary>State transition: clearing the filter resets the text, the status and the cancelled choice, and loads again.</summary>
    [Fact]
    public async Task ClearFilterAsync_FiltersSet_ResetsAndLoads()
    {
        OverviewIs(BookingItem(1));
        var viewModel = Create();
        viewModel.SearchText = "rita";
        viewModel.StatusFilter = BookingStatus.Confirmed;
        viewModel.IncludeCancelled = true;

        await viewModel.ClearFilterAsync(Token);

        Assert.Null(viewModel.SearchText);
        Assert.Null(viewModel.StatusFilter);
        Assert.False(viewModel.IncludeCancelled);
        Assert.Single(viewModel.Bookings);
    }

    #endregion

    #region SortByAsync

    /// <summary>State transition: the same column twice turns the order around; a new column starts ascending.</summary>
    [Fact]
    public async Task SortByAsync_SameColumnThenOther_TogglesThenResets()
    {
        OverviewIs();
        var viewModel = Create();
        Assert.Equal("none", viewModel.AriaSortFor(BookingSortField.GuestName));

        await viewModel.SortByAsync(BookingSortField.GuestName, Token);
        Assert.True(viewModel.IsSortedBy(BookingSortField.GuestName));
        Assert.Equal("ascending", viewModel.AriaSortFor(BookingSortField.GuestName));

        await viewModel.SortByAsync(BookingSortField.GuestName, Token);
        Assert.Equal(SortDirection.Descending, viewModel.Direction);
        Assert.Equal("descending", viewModel.AriaSortFor(BookingSortField.GuestName));

        await viewModel.SortByAsync(BookingSortField.Nights, Token);
        Assert.Equal(SortDirection.Ascending, viewModel.Direction);
        Assert.False(viewModel.IsSortedBy(BookingSortField.GuestName));
    }

    #endregion

    #region ExecuteAsync 1

    /// <summary>Equivalence: each action calls its own use case and loads the list again.</summary>
    [Theory]
    [InlineData(BookingAction.Confirm)]
    [InlineData(BookingAction.CheckIn)]
    [InlineData(BookingAction.CheckOut)]
    [InlineData(BookingAction.Cancel)]
    public async Task ExecuteAsync_Action_CallsItsUseCaseAndReloads(BookingAction action)
    {
        OverviewIs(BookingItem(5));
        _bookings.ConfirmAsync(5, Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(5)));
        _bookings.CheckInAsync(5, Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(5)));
        _bookings.CheckOutAsync(5, Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(5)));
        _bookings.CancelAsync(5, Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(5)));
        var viewModel = Create();

        await viewModel.ExecuteAsync(action, 5, Token);

        Assert.Empty(viewModel.Errors);
        Assert.Single(viewModel.Bookings);
        var calls = _bookings.ReceivedCalls().Select(call => call.GetMethodInfo().Name).ToArray();
        Assert.Contains(action + "Async", calls);
        Assert.Contains(nameof(IBookingService.GetOverviewAsync), calls);
    }

    /// <summary>Negative: an action that does not exist is refused.</summary>
    [Fact]
    public async Task ExecuteAsync_UnknownAction_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Create().ExecuteAsync((BookingAction)99, 1, Token));
    }

    #endregion

    #region ConfirmCancelAsync

    /// <summary>Error handling: a refused action shows the text, clears a pending cancellation and does not reload.</summary>
    [Fact]
    public async Task ConfirmCancelAsync_Refused_ShowsTheErrorAndClearsTheQuestion()
    {
        _bookings.CancelAsync(5, Arg.Any<CancellationToken>()).Returns(Fail<BookingDetailsDto>(ErrorCodes.Booking.ConcurrencyConflict));
        var viewModel = Create();
        viewModel.RequestCancel(BookingItem(5));

        await viewModel.ConfirmCancelAsync(Token);

        Assert.Equal([Text(ErrorCodes.Booking.ConcurrencyConflict)], viewModel.Errors);
        Assert.Null(viewModel.BookingPendingCancellation);
        await _bookings.DidNotReceiveWithAnyArgs().GetOverviewAsync(default!, Arg.Any<CancellationToken>());
    }

    #endregion

    #region RequestCancel

    /// <summary>State transition: a cancellation is asked for, can be aborted, and without a question nothing is cancelled.</summary>
    [Fact]
    public async Task RequestCancel_ThenAbort_CancelsNothing()
    {
        var viewModel = Create();
        viewModel.RequestCancel(BookingItem(5));
        Assert.Equal(5, viewModel.BookingPendingCancellation!.BookingId);

        viewModel.AbortCancel();
        await viewModel.ConfirmCancelAsync(Token);

        Assert.Null(viewModel.BookingPendingCancellation);
        await _bookings.DidNotReceiveWithAnyArgs().CancelAsync(default, Arg.Any<CancellationToken>());
        Assert.Throws<ArgumentNullException>(() => viewModel.RequestCancel(null!));
    }

    #endregion

    #region ExecuteAsync 2

    /// <summary>Concurrency: an action while a load runs is ignored.</summary>
    [Fact]
    public async Task ExecuteAsync_WhileLoading_IsIgnored()
    {
        var pending = new TaskCompletionSource<NFHotel.Application.UseCases.Result<IReadOnlyList<BookingListItemDto>>>();
        _bookings.GetOverviewAsync(Arg.Any<BookingOverviewQuery>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var viewModel = Create();

        var load = viewModel.SearchAsync(Token);
        await viewModel.ExecuteAsync(BookingAction.Confirm, 5, Token);
        pending.SetResult(NFHotel.Application.UseCases.Result<IReadOnlyList<BookingListItemDto>>.Success([]));
        await load;

        await _bookings.DidNotReceiveWithAnyArgs().ConfirmAsync(default, Arg.Any<CancellationToken>());
    }

    #endregion
}
