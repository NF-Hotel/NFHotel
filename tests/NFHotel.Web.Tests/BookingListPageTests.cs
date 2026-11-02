using Bunit;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;
using NFHotel.Web.Features.Admin.Bookings;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>The booking page of the back office, rendered and clicked (MIL-054 task 2).</summary>
public sealed class BookingListPageTests : IAsyncDisposable
{
    #region Setup

    private readonly IBookingService _bookings = Substitute.For<IBookingService>();
    private readonly IGuestService _guests = Substitute.For<IGuestService>();
    private readonly PageHost _host = new();

    public BookingListPageTests()
    {
        var clock = new FakeClock { Today = Today };
        var bookings = new PassThroughRunner<IBookingService>(_bookings);
        _host.With(new BookingListViewModel(bookings, clock))
            .With(new BookingFormViewModel(bookings, new PassThroughRunner<IGuestService>(_guests), clock));
        _bookings.GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(List(FreeRoom(1), FreeRoom(2)));
        _guests.SearchAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(List(Guest(4)));
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private void OverviewIs(params BookingListItemDto[] bookings) =>
        _bookings.GetOverviewAsync(Arg.Any<BookingOverviewQuery>(), Arg.Any<CancellationToken>()).Returns(List(bookings));

    #endregion

    #region Render 1

    /// <summary>Positive: the list shows each booking with only the buttons its actions allow.</summary>
    [Fact]
    public void Render_Bookings_ShowsRowsAndAllowedButtons()
    {
        OverviewIs(BookingItem(1, BookingStatus.Pending), BookingItem(2, BookingStatus.CheckedOut, NoActions));

        var page = _host.Render<BookingList>();

        Assert.Contains("2 bookings in this period", page.Markup, StringComparison.Ordinal);
        Assert.Contains("B-0001", page.Markup, StringComparison.Ordinal);
        Assert.Contains("Checked out", page.Markup, StringComparison.Ordinal);
        Assert.Equal(1, page.FindAll("button").Count(button => button.TextContent.Trim() == "Confirm"));
        Assert.True(page.HasButton("Reschedule"));
    }

    #endregion

    #region Click

    /// <summary>Equivalence: each row action and each sort column calls the view model, which loads again.</summary>
    [Fact]
    public void Click_ActionsAndSortColumns_CallTheUseCases()
    {
        OverviewIs(BookingItem(1));
        _bookings.ConfirmAsync(1, Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(1)));
        _bookings.CheckInAsync(1, Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(1)));
        _bookings.CheckOutAsync(1, Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(1)));
        var page = _host.Render<BookingList>();

        page.Button("Confirm").Click();
        page.Button("Check in").Click();
        page.Button("Check out").Click();
        foreach (var column in new[] { "Booking", "Guest", "Room", "Check-in", "Check-out", "Nights", "Status" })
        {
            page.Button(column).Click();
        }

        _bookings.Received(1).ConfirmAsync(1, Arg.Any<CancellationToken>());
        _bookings.Received(1).CheckInAsync(1, Arg.Any<CancellationToken>());
        _bookings.Received(1).CheckOutAsync(1, Arg.Any<CancellationToken>());
        Assert.Equal("ascending", page.Find("th[aria-sort='ascending']").GetAttribute("aria-sort"));
    }

    #endregion

    #region Cancel

    /// <summary>State transition: a cancellation asks first, can be aborted, and cancels on confirm.</summary>
    [Fact]
    public void Cancel_AskThenAbortThenConfirm_CancelsOnce()
    {
        OverviewIs(BookingItem(1));
        _bookings.CancelAsync(1, Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(1, BookingStatus.Cancelled)));
        var page = _host.Render<BookingList>();

        page.Button("Cancel").Click();
        Assert.Contains("Cancel booking B-0001?", page.Markup, StringComparison.Ordinal);
        page.Button("Go back").Click();
        Assert.DoesNotContain("Cancel booking B-0001?", page.Markup, StringComparison.Ordinal);

        page.Button("Cancel").Click();
        page.Button("Cancel booking").Click();

        _bookings.Received(1).CancelAsync(1, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Render 2

    /// <summary>Edge: an empty period offers to create a booking or clear the filter; a status with no rows offers all statuses.</summary>
    [Fact]
    public void Render_EmptyAndFiltered_ShowTheirOwnEmptyStates()
    {
        OverviewIs();
        var page = _host.Render<BookingList>();
        Assert.Contains("No bookings in this period", page.Markup, StringComparison.Ordinal);
        page.FindAll("button").Last(button => button.TextContent.Trim() == "Clear filter").Click();

        OverviewIs(BookingItem(1, BookingStatus.Pending));
        page.Button("Search").Click();
        page.Find("#filter-status").Change(nameof(BookingStatus.Cancelled));
        Assert.Contains("No bookings with that status", page.Markup, StringComparison.Ordinal);

        page.Button("Show all statuses").Click();
        Assert.Contains("B-0001", page.Markup, StringComparison.Ordinal);
    }

    /// <summary>Error handling: a refused load shows the error text.</summary>
    [Fact]
    public void Render_Refused_ShowsTheError()
    {
        _bookings.GetOverviewAsync(Arg.Any<BookingOverviewQuery>(), Arg.Any<CancellationToken>())
            .Returns(Fail<IReadOnlyList<BookingListItemDto>>(ErrorCodes.Booking.NotFound));

        var page = _host.Render<BookingList>();

        Assert.Contains(Text(ErrorCodes.Booking.NotFound), page.Markup, StringComparison.Ordinal);
    }

    #endregion

    #region Create

    /// <summary>Positive: a booking is created with an existing guest through the form, and the list is loaded again.</summary>
    [Fact]
    public void Create_WithExistingGuest_SavesAndShowsTheReceipt()
    {
        OverviewIs();
        _bookings.CreateAsync(Arg.Any<CreateBookingRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(9)));
        var page = _host.Render<BookingList>();

        page.Button("New booking").Click();
        Assert.Contains("New booking", page.Find("#booking-form").TextContent, StringComparison.Ordinal);
        page.Find("#form-check-in").Change(Today.AddDays(1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        page.Find("#form-check-out").Change(Today.AddDays(3).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        page.Button("Find available rooms").Click();
        page.Button("Choose room 101").Click();
        Assert.Contains("Selected", page.Markup, StringComparison.Ordinal);
        page.Find("#form-guest-search").Change("rita");
        page.Button("Search guests").Click();
        page.Button("Choose Rita Hansen").Click();
        page.Button("Create booking").Click();

        Assert.Contains("Booking B-0009 was created.", page.Markup, StringComparison.Ordinal);
        _bookings.Received(1).CreateAsync(new CreateBookingRequest(Today.AddDays(1), Today.AddDays(3), 1, 4, null), Arg.Any<CancellationToken>());
        _bookings.Received(2).GetOverviewAsync(Arg.Any<BookingOverviewQuery>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Positive: a new guest is typed into the form and validated as it is typed.</summary>
    [Fact]
    public void Create_WithNewGuest_ValidatesWhileTyping()
    {
        OverviewIs();
        var page = _host.Render<BookingList>();
        page.Button("New booking").Click();

        page.Button("New guest").Click();
        Assert.Equal("true", page.Button("New guest").GetAttribute("aria-pressed"));
        foreach (var field in new[] { "#form-first-name", "#form-last-name", "#form-email", "#form-phone", "#form-country", "#form-passport" })
        {
            page.Find(field).Input("x");
        }

        _guests.Received(7).ValidateFields(Arg.Any<GuestFields>());
        page.Button("Existing guest").Click();
        Assert.Equal("false", page.Button("New guest").GetAttribute("aria-pressed"));
    }

    #endregion

    #region Form

    /// <summary>Edge: no free rooms and no matching guests each say so; cancel closes the form.</summary>
    [Fact]
    public void Form_NothingFound_SaysSoAndCloses()
    {
        OverviewIs();
        _bookings.GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(List<AvailableRoomDto>());
        _guests.SearchAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(List<GuestListItemDto>());
        var page = _host.Render<BookingList>();
        page.Button("New booking").Click();

        page.Button("Find available rooms").Click();
        page.Button("Search guests").Click();

        Assert.Contains("No rooms are free for those dates", page.Markup, StringComparison.Ordinal);
        Assert.Contains("No guests match that search", page.Markup, StringComparison.Ordinal);
        page.FindAll("#booking-form ~ * button, section button").First(button => button.TextContent.Trim() == "Cancel").Click();
        Assert.Empty(page.FindAll("#booking-form"));
    }

    #endregion

    #region Reschedule

    /// <summary>Positive: a booking is rescheduled from its row; the form has no guest part.</summary>
    [Fact]
    public void Reschedule_FromTheRow_SavesTheNewDates()
    {
        OverviewIs(BookingItem(1));
        _bookings.RescheduleAsync(Arg.Any<RescheduleBookingRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(1)));
        var page = _host.Render<BookingList>();

        page.Button("Reschedule").Click();
        Assert.Contains("Reschedule booking B-0001", page.Markup, StringComparison.Ordinal);
        Assert.False(page.HasButton("New guest"));
        page.Button("Save new dates").Click();

        Assert.Contains("Booking B-0001 was moved to the new dates.", page.Markup, StringComparison.Ordinal);
    }

    #endregion

    #region Search

    /// <summary>Positive: the filter fields are sent with the search.</summary>
    [Fact]
    public void Search_FilterFields_AreSent()
    {
        OverviewIs();
        var page = _host.Render<BookingList>();

        page.Find("#period-start").Change("2026-04-01");
        page.Find("#period-end").Change("2026-04-30");
        page.Find("#search-text").Change("rita");
        page.Find("#include-cancelled").Change(true);
        page.Button("Search").Click();

        _bookings.Received().GetOverviewAsync(
            Arg.Is<BookingOverviewQuery>(query =>
                query.PeriodStart == new DateOnly(2026, 4, 1)
                && query.PeriodEnd == new DateOnly(2026, 4, 30)
                && query.SearchText == "rita"
                && query.IncludeCancelled),
            Arg.Any<CancellationToken>());
    }

    #endregion
}
