using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.TestSupport;
using NFHotel.Web.Features.Admin.Bookings;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>The presentation logic of the form that creates and reschedules a booking (MIL-054 task 2).</summary>
public sealed class BookingFormViewModelTests
{
    #region Setup

    private readonly IBookingService _bookings = Substitute.For<IBookingService>();
    private readonly IGuestService _guests = Substitute.For<IGuestService>();
    private readonly FakeClock _clock = new() { Today = Today };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private BookingFormViewModel Create() =>
        new(new PassThroughRunner<IBookingService>(_bookings), new PassThroughRunner<IGuestService>(_guests), _clock);

    private void RoomsAre(params AvailableRoomDto[] rooms) =>
        _bookings.GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(List(rooms));

    #endregion

    #region Constructor

    /// <summary>Negative: each dependency is required.</summary>
    [Fact]
    public void Constructor_MissingDependency_Throws()
    {
        var bookings = new PassThroughRunner<IBookingService>(_bookings);
        var guests = new PassThroughRunner<IGuestService>(_guests);

        Assert.Throws<ArgumentNullException>(() => new BookingFormViewModel(null!, guests, _clock));
        Assert.Throws<ArgumentNullException>(() => new BookingFormViewModel(bookings, null!, _clock));
        Assert.Throws<ArgumentNullException>(() => new BookingFormViewModel(bookings, guests, null!));
    }

    /// <summary>Positive: a new form starts today for one night, closed, with nothing chosen.</summary>
    [Fact]
    public void Constructor_NewForm_StartsTodayForOneNight()
    {
        var viewModel = Create();

        Assert.Equal(Today, viewModel.CheckInDate);
        Assert.Equal(Today.AddDays(1), viewModel.CheckOutDate);
        Assert.False(viewModel.IsOpen);
        Assert.Equal("New booking", viewModel.Title);
        Assert.False(viewModel.CanSave);
        Assert.False(viewModel.HasNoAvailableRooms);
    }

    #endregion

    #region BeginCreate

    /// <summary>State transition: opening for creation resets what the previous opening chose.</summary>
    [Fact]
    public async Task BeginCreate_AfterReschedule_ResetsTheForm()
    {
        RoomsAre(FreeRoom(1));
        var viewModel = Create();
        await viewModel.BeginRescheduleAsync(BookingItem(7), Token);
        _clock.Today = Today.AddDays(3);

        viewModel.BeginCreate();

        Assert.True(viewModel.IsOpen);
        Assert.Equal(BookingFormMode.Create, viewModel.Mode);
        Assert.Null(viewModel.BookingId);
        Assert.Null(viewModel.BookingNumber);
        Assert.Null(viewModel.SelectedRoomId);
        Assert.Empty(viewModel.AvailableRooms);
        Assert.Equal(Today.AddDays(3), viewModel.CheckInDate);
        Assert.Equal(Today.AddDays(4), viewModel.CheckOutDate);
    }

    #endregion

    #region BeginRescheduleAsync

    /// <summary>Positive: rescheduling takes the booking's dates and room and searches at once, leaving the booking out.</summary>
    [Fact]
    public async Task BeginRescheduleAsync_Booking_SearchesWithoutTheBookingItself()
    {
        RoomsAre(FreeRoom(1), FreeRoom(2));
        var viewModel = Create();

        await viewModel.BeginRescheduleAsync(BookingItem(7), Token);

        Assert.Equal(BookingFormMode.Reschedule, viewModel.Mode);
        Assert.Equal("Reschedule booking B-0007", viewModel.Title);
        Assert.Equal(1, viewModel.SelectedRoomId);
        Assert.True(viewModel.HasSearchedRooms);
        Assert.Equal(2, viewModel.AvailableRooms.Count);
        Assert.True(viewModel.CanSave);
        await _bookings.Received(1).GetAvailableRoomsAsync(Today, Today.AddDays(2), 7, Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: a missing booking is refused.</summary>
    [Fact]
    public async Task BeginRescheduleAsync_NullBooking_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => Create().BeginRescheduleAsync(null!, Token));
    }

    #endregion

    #region SearchRoomsAsync

    /// <summary>Edge: the room of the booking is taken in the new period, so it is no longer chosen.</summary>
    [Fact]
    public async Task SearchRoomsAsync_ChosenRoomNotFree_ClearsTheChoice()
    {
        RoomsAre(FreeRoom(2));
        var viewModel = Create();

        await viewModel.BeginRescheduleAsync(BookingItem(7, roomId: 1), Token);

        Assert.Null(viewModel.SelectedRoomId);
        Assert.False(viewModel.CanSave);
    }

    /// <summary>Edge: a search that finds no free room says so.</summary>
    [Fact]
    public async Task SearchRoomsAsync_NoFreeRooms_HasNoAvailableRooms()
    {
        RoomsAre();
        var viewModel = Create();

        await viewModel.SearchRoomsAsync(Token);

        Assert.True(viewModel.HasNoAvailableRooms);
    }

    /// <summary>Error handling: a refused search shows the text, empties the rooms and clears the choice.</summary>
    [Fact]
    public async Task SearchRoomsAsync_Refused_ShowsTheErrorAndClearsTheRooms()
    {
        var viewModel = Create();
        viewModel.SelectRoom(3);
        _bookings.GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Fail<IReadOnlyList<AvailableRoomDto>>(ErrorCodes.Booking.NotFound));

        await viewModel.SearchRoomsAsync(Token);

        Assert.Equal([Text(ErrorCodes.Booking.NotFound)], viewModel.Errors);
        Assert.Empty(viewModel.AvailableRooms);
        Assert.Null(viewModel.SelectedRoomId);
        Assert.False(viewModel.HasNoAvailableRooms);
    }

    #endregion

    #region Calls

    /// <summary>Concurrency: a second call while one runs is ignored, for every call that goes to a service.</summary>
    [Fact]
    public async Task Calls_WhileBusy_AreIgnored()
    {
        var pending = new TaskCompletionSource<Result<IReadOnlyList<AvailableRoomDto>>>();
        _bookings.GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(pending.Task);
        var viewModel = Create();
        viewModel.SelectRoom(1);
        viewModel.SelectGuest(1);

        var first = viewModel.SearchRoomsAsync(Token);
        Assert.True(viewModel.IsBusy);
        Assert.False(viewModel.CanSave);
        await viewModel.SearchRoomsAsync(Token);
        await viewModel.SearchGuestsAsync(Token);
        await viewModel.SaveAsync(Token);
        pending.SetResult(Result<IReadOnlyList<AvailableRoomDto>>.Success([FreeRoom(1)]));
        await first;

        Assert.False(viewModel.IsBusy);
        await _bookings.Received(1).GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
        await _guests.DidNotReceive().SearchAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>());
        await _bookings.DidNotReceive().CreateAsync(Arg.Any<CreateBookingRequest>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region SearchGuestsAsync

    /// <summary>Positive and error handling: the guest search lists the guests, and a refusal empties the list.</summary>
    [Fact]
    public async Task SearchGuestsAsync_FoundThenRefused_ListsThenShowsTheError()
    {
        _guests.SearchAsync("rita", Arg.Any<CancellationToken>())
            .Returns(List(Guest(1)), Fail<IReadOnlyList<GuestListItemDto>>(ErrorCodes.Guest.NotFound));
        var viewModel = Create();
        viewModel.GuestSearchText = "rita";

        await viewModel.SearchGuestsAsync(Token);
        Assert.True(viewModel.HasSearchedGuests);
        Assert.Single(viewModel.Guests);
        Assert.Empty(viewModel.Errors);

        await viewModel.SearchGuestsAsync(Token);
        Assert.Empty(viewModel.Guests);
        Assert.Equal([Text(ErrorCodes.Guest.NotFound)], viewModel.Errors);
    }

    #endregion

    #region SetGuestMode

    /// <summary>State transition: switching guest mode resets the other choice and validates a new guest at once.</summary>
    [Fact]
    public void SetGuestMode_NewThenExisting_ResetsTheOtherChoice()
    {
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Failure(ErrorCodes.Guest.NotFound));
        var viewModel = Create();
        viewModel.SelectRoom(1);
        viewModel.SelectGuest(4);

        viewModel.SetGuestMode(BookingGuestMode.New);
        Assert.Null(viewModel.SelectedGuestId);
        Assert.Equal([Text(ErrorCodes.Guest.NotFound)], viewModel.GuestErrors);
        Assert.False(viewModel.CanSave);

        viewModel.SetGuestMode(BookingGuestMode.Existing);
        Assert.Empty(viewModel.GuestErrors);
        Assert.False(viewModel.NewGuestIsValid);
    }

    #endregion

    #region ValidateNewGuest

    /// <summary>Positive: valid fields of a new guest let the form be saved; an empty passport is sent as missing.</summary>
    [Fact]
    public void ValidateNewGuest_ValidFields_CanSave()
    {
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        var viewModel = Create();
        viewModel.SelectRoom(1);
        viewModel.SetGuestMode(BookingGuestMode.New);
        viewModel.FirstName = "Rita";
        viewModel.PassportNumber = " ";

        viewModel.ValidateNewGuest();

        Assert.True(viewModel.NewGuestIsValid);
        Assert.True(viewModel.CanSave);
        _guests.Received().ValidateFields(Arg.Is<GuestFields>(fields => fields.FirstName == "Rita" && fields.PassportNumber == null));
    }

    #endregion

    #region SaveAsync

    /// <summary>Positive: creating with an existing guest sends the guest id only, closes the form and keeps the receipt.</summary>
    [Fact]
    public async Task SaveAsync_CreateWithExistingGuest_SendsTheGuestIdAndCloses()
    {
        _bookings.CreateAsync(Arg.Any<CreateBookingRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(9)));
        var viewModel = Create();
        viewModel.BeginCreate();
        viewModel.SelectRoom(2);
        viewModel.SelectGuest(4);

        await viewModel.SaveAsync(Token);

        Assert.True(viewModel.SavedSuccessfully);
        Assert.False(viewModel.IsOpen);
        Assert.Equal("Booking B-0009 was created.", viewModel.SuccessMessage);
        await _bookings.Received(1).CreateAsync(
            new CreateBookingRequest(Today, Today.AddDays(1), 2, 4, null),
            Arg.Any<CancellationToken>());

        viewModel.DismissSuccess();
        Assert.Null(viewModel.SuccessMessage);
    }

    /// <summary>Positive: creating with a new guest sends the fields only, never a guest id as well (BR-47).</summary>
    [Fact]
    public async Task SaveAsync_CreateWithNewGuest_SendsTheFieldsOnly()
    {
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        _bookings.CreateAsync(Arg.Any<CreateBookingRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(9)));
        var viewModel = Create();
        viewModel.BeginCreate();
        viewModel.SelectRoom(2);
        viewModel.SetGuestMode(BookingGuestMode.New);
        viewModel.FirstName = "Rita";
        viewModel.LastName = "Hansen";
        viewModel.Email = "rita@example.com";
        viewModel.PhoneNumber = "+4512345678";
        viewModel.Country = "Denmark";
        viewModel.PassportNumber = "P123";

        await viewModel.SaveAsync(Token);

        await _bookings.Received(1).CreateAsync(
            new CreateBookingRequest(
                Today,
                Today.AddDays(1),
                2,
                null,
                new GuestFields("Rita", "Hansen", "rita@example.com", "+4512345678", "Denmark", "P123")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Positive: a reschedule sends the booking, the new dates and the room, and says the booking moved.</summary>
    [Fact]
    public async Task SaveAsync_Reschedule_SendsTheNewPeriod()
    {
        RoomsAre(FreeRoom(1));
        _bookings.RescheduleAsync(Arg.Any<RescheduleBookingRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(7)));
        var viewModel = Create();
        await viewModel.BeginRescheduleAsync(BookingItem(7), Token);
        viewModel.CheckOutDate = Today.AddDays(5);

        await viewModel.SaveAsync(Token);

        Assert.Equal("Booking B-0007 was moved to the new dates.", viewModel.SuccessMessage);
        await _bookings.Received(1).RescheduleAsync(
            new RescheduleBookingRequest(7, Today, Today.AddDays(5), 1),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Error handling: a refused save keeps the form open with the text and the choices.</summary>
    [Fact]
    public async Task SaveAsync_Refused_KeepsTheFormOpenWithTheError()
    {
        _bookings.CreateAsync(Arg.Any<CreateBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Fail<BookingDetailsDto>(ErrorCodes.Booking.ConcurrencyConflict));
        var viewModel = Create();
        viewModel.BeginCreate();
        viewModel.SelectRoom(2);
        viewModel.SelectGuest(4);

        await viewModel.SaveAsync(Token);

        Assert.False(viewModel.SavedSuccessfully);
        Assert.True(viewModel.IsOpen);
        Assert.Equal(2, viewModel.SelectedRoomId);
        Assert.Equal([Text(ErrorCodes.Booking.ConcurrencyConflict)], viewModel.Errors);
    }

    /// <summary>Negative: without a room nothing is sent.</summary>
    [Fact]
    public async Task SaveAsync_NoRoom_SendsNothing()
    {
        var viewModel = Create();
        viewModel.BeginCreate();

        await viewModel.SaveAsync(Token);

        Assert.False(viewModel.SavedSuccessfully);
        await _bookings.DidNotReceiveWithAnyArgs().CreateAsync(default!, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Close

    /// <summary>State transition: closing resets the form.</summary>
    [Fact]
    public void Close_OpenForm_ClosesAndResets()
    {
        var viewModel = Create();
        viewModel.BeginCreate();
        viewModel.SelectRoom(2);
        viewModel.GuestSearchText = "rita";

        viewModel.Close();

        Assert.False(viewModel.IsOpen);
        Assert.Null(viewModel.SelectedRoomId);
        Assert.Null(viewModel.GuestSearchText);
    }

    #endregion
}
