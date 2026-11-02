using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;
using NFHotel.Web.Features.Booking;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>The public booking flow of a guest: dates, room, guest, receipt (MIL-054 task 3).</summary>
public sealed class BookingFlowViewModelTests
{
    #region Setup

    private readonly IBookingService _bookings = Substitute.For<IBookingService>();
    private readonly IGuestService _guests = Substitute.For<IGuestService>();
    private readonly FakeClock _clock = new() { Today = Today };

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private BookingFlowViewModel Create() =>
        new(new PassThroughRunner<IBookingService>(_bookings), new PassThroughRunner<IGuestService>(_guests), _clock);

    private void RoomsAre(params AvailableRoomDto[] rooms) =>
        _bookings.GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(List(rooms));

    private async Task<BookingFlowViewModel> AtGuestStepAsync()
    {
        RoomsAre(FreeRoom(1), FreeRoom(2));
        var viewModel = Create();
        await viewModel.FindRoomsAsync(Token);
        viewModel.SelectRoom(1);
        viewModel.FirstName = " Rita ";
        viewModel.LastName = "Hansen";
        viewModel.Email = "rita@example.com";
        viewModel.PhoneNumber = "+4512345678";
        viewModel.Country = "Denmark";
        return viewModel;
    }

    #endregion

    #region Constructor

    /// <summary>Negative: each dependency is required.</summary>
    [Fact]
    public void Constructor_MissingDependency_Throws()
    {
        var bookings = new PassThroughRunner<IBookingService>(_bookings);
        var guests = new PassThroughRunner<IGuestService>(_guests);

        Assert.Throws<ArgumentNullException>(() => new BookingFlowViewModel(null!, guests, _clock));
        Assert.Throws<ArgumentNullException>(() => new BookingFlowViewModel(bookings, null!, _clock));
        Assert.Throws<ArgumentNullException>(() => new BookingFlowViewModel(bookings, guests, null!));
    }

    /// <summary>Positive: the flow starts at the dates, tomorrow for one night, with no error shown yet.</summary>
    [Fact]
    public void Constructor_NewFlow_StartsTomorrowForOneNight()
    {
        var viewModel = Create();

        Assert.Equal(BookingFlowStep.Dates, viewModel.CurrentStep);
        Assert.Equal(Today.AddDays(1), viewModel.CheckInDate);
        Assert.Equal(Today.AddDays(2), viewModel.CheckOutDate);
        Assert.True(viewModel.HasValidDates);
        Assert.True(viewModel.CanFindRooms);
        Assert.False(viewModel.CanSubmit);
        Assert.Equal(1, viewModel.Nights);
        Assert.Equal("1 night", viewModel.NightsText);
        Assert.Equal("11 March 2026 – 12 March 2026", viewModel.PeriodText);
        Assert.Equal(string.Empty, viewModel.SelectionSummary);
        Assert.Null(viewModel.CheckInError);
        Assert.Null(viewModel.CheckOutError);
        Assert.Equal("Payment on arrival", BookingFlowViewModel.PaymentText);
    }

    #endregion

    #region FindRoomsAsync 1

    /// <summary>Negative: missing dates are only reported after an attempt, and keep the flow at the dates.</summary>
    [Fact]
    public async Task FindRoomsAsync_MissingDates_StaysAtTheDatesWithErrors()
    {
        var viewModel = Create();
        viewModel.CheckInDate = null;
        viewModel.CheckOutDate = default(DateOnly);
        Assert.Null(viewModel.CheckInError);

        await viewModel.FindRoomsAsync(Token);

        Assert.Equal(BookingFlowStep.Dates, viewModel.CurrentStep);
        Assert.Equal(Text(ErrorCodes.Booking.StartDateRequired), viewModel.CheckInError);
        Assert.Equal(Text(ErrorCodes.Booking.EndDateRequired), viewModel.CheckOutError);
        Assert.Equal(0, viewModel.Nights);
        Assert.Equal(string.Empty, viewModel.PeriodText);
        Assert.StartsWith("Pick your check-in", viewModel.NightsText, StringComparison.Ordinal);
        Assert.Empty(_bookings.ReceivedCalls());
    }

    #endregion

    #region Dates

    /// <summary>Boundary: a check-in yesterday and a check-out on the check-in day are each refused with their text.</summary>
    [Fact]
    public void Dates_PastStartAndZeroNights_ExplainBoth()
    {
        var viewModel = Create();
        viewModel.CheckInDate = Today.AddDays(-1);
        viewModel.CheckOutDate = Today.AddDays(-1);

        Assert.False(viewModel.HasValidDates);
        Assert.Equal(Text(ErrorCodes.Booking.StartDateInPast), viewModel.CheckInError);
        Assert.Equal(Text(ErrorCodes.Booking.EndNotAfterStart), viewModel.CheckOutError);

        viewModel.CheckInDate = Today;
        viewModel.CheckOutDate = Today.AddDays(3);
        Assert.True(viewModel.HasValidDates);
        Assert.Equal("3 nights", viewModel.NightsText);
    }

    #endregion

    #region FindRoomsAsync 2

    /// <summary>Positive: valid dates find the free rooms and move on to the room step.</summary>
    [Fact]
    public async Task FindRoomsAsync_ValidDates_ShowsTheRooms()
    {
        RoomsAre(FreeRoom(1));
        var viewModel = Create();

        await viewModel.FindRoomsAsync(Token);

        Assert.Equal(BookingFlowStep.Room, viewModel.CurrentStep);
        Assert.True(viewModel.AreRoomsLoaded);
        Assert.Single(viewModel.AvailableRooms);
        Assert.Equal("step step-done", viewModel.StepClass(BookingFlowStep.Dates));
        Assert.Equal("step step-active", viewModel.StepClass(BookingFlowStep.Room));
        Assert.Equal("step", viewModel.StepClass(BookingFlowStep.Guest));
        Assert.Equal("step", viewModel.StepAria(BookingFlowStep.Room));
        Assert.Null(viewModel.StepAria(BookingFlowStep.Dates));
        await _bookings.Received(1).GetAvailableRoomsAsync(Today.AddDays(1), Today.AddDays(2), null, Arg.Any<CancellationToken>());
    }

    /// <summary>Error handling: a refused search stays at the dates with the text.</summary>
    [Fact]
    public async Task FindRoomsAsync_Refused_ShowsTheError()
    {
        _bookings.GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Fail<IReadOnlyList<AvailableRoomDto>>(ErrorCodes.Booking.StartDateInPast));
        var viewModel = Create();

        await viewModel.FindRoomsAsync(Token);

        Assert.Equal(BookingFlowStep.Dates, viewModel.CurrentStep);
        Assert.False(viewModel.AreRoomsLoaded);
        Assert.Equal([Text(ErrorCodes.Booking.StartDateInPast)], viewModel.Errors);
    }

    /// <summary>Edge: a room chosen for earlier dates is dropped when it is not free for the new ones.</summary>
    [Fact]
    public async Task FindRoomsAsync_ChosenRoomNoLongerFree_DropsIt()
    {
        var viewModel = await AtGuestStepAsync();
        RoomsAre(FreeRoom(2));

        await viewModel.FindRoomsAsync(Token);

        Assert.Null(viewModel.SelectedRoom);
    }

    #endregion

    #region SelectRoom

    /// <summary>Positive: choosing a room moves on to the guest and sums up the choice.</summary>
    [Fact]
    public async Task SelectRoom_FreeRoom_MovesToTheGuest()
    {
        var viewModel = await AtGuestStepAsync();

        Assert.Equal(BookingFlowStep.Guest, viewModel.CurrentStep);
        Assert.True(viewModel.CanSubmit);
        Assert.Equal(
            "Room 101 · Double room · Floor 1 · sleeps 2 · 11 March 2026 – 12 March 2026 · 1 night · Payment on arrival",
            viewModel.SelectionSummary);
    }

    /// <summary>Negative: a room that is not in the list is ignored, and invalid dates send the guest back.</summary>
    [Fact]
    public async Task SelectRoom_UnknownRoomOrInvalidDates_IsIgnoredOrSentBack()
    {
        RoomsAre(FreeRoom(1));
        var viewModel = Create();
        await viewModel.FindRoomsAsync(Token);

        viewModel.SelectRoom(9);
        Assert.Equal(BookingFlowStep.Room, viewModel.CurrentStep);

        viewModel.CheckInDate = null;
        viewModel.SelectRoom(1);
        Assert.Equal(BookingFlowStep.Dates, viewModel.CurrentStep);
        Assert.Null(viewModel.SelectedRoom);
    }

    #endregion

    #region DescribeRoom

    /// <summary>Equivalence: the room is described with its size, floor and how many it sleeps.</summary>
    [Fact]
    public void DescribeRoom_Room_ListsItsFacts()
    {
        Assert.Equal("sleeps 1", BookingFlowViewModel.DescribeCapacity(1));
        Assert.Equal("sleeps 3", BookingFlowViewModel.DescribeCapacity(3));
        Assert.Equal("Floor 4", BookingFlowViewModel.DescribeFloor(4));
        Assert.Throws<ArgumentNullException>(() => BookingFlowViewModel.DescribeRoom(null!));
    }

    #endregion

    #region BackToRoomsAndDates

    /// <summary>State transition: back to the rooms needs loaded rooms, otherwise it goes to the dates.</summary>
    [Fact]
    public async Task BackToRoomsAndDates_FromTheGuest_GoBack()
    {
        var viewModel = await AtGuestStepAsync();

        viewModel.BackToRooms();
        Assert.Equal(BookingFlowStep.Room, viewModel.CurrentStep);
        viewModel.BackToDates();
        Assert.Equal(BookingFlowStep.Dates, viewModel.CurrentStep);

        var fresh = Create();
        fresh.BackToRooms();
        Assert.Equal(BookingFlowStep.Dates, fresh.CurrentStep);
    }

    #endregion

    #region SubmitAsync

    /// <summary>Negative: invalid guest fields are placed on their fields, and nothing is booked.</summary>
    [Fact]
    public async Task SubmitAsync_InvalidGuest_PlacesTheErrorsOnTheFields()
    {
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Failure(
            [new Error(ErrorCodes.Guest.FirstNameRequired), new Error(ErrorCodes.Guest.EmailInvalid), new Error(ErrorCodes.Booking.NotFound)]));
        var viewModel = await AtGuestStepAsync();

        await viewModel.SubmitAsync(Token);

        Assert.Equal(Text(ErrorCodes.Guest.FirstNameRequired), viewModel.GuestErrors.FirstName);
        Assert.Equal(Text(ErrorCodes.Guest.EmailInvalid), viewModel.GuestErrors.Email);
        Assert.Equal([Text(ErrorCodes.Booking.NotFound)], viewModel.Errors);
        await _bookings.DidNotReceiveWithAnyArgs().CreatePublicAsync(Arg.Any<CreateBookingRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Positive: a valid booking is sent as a new guest with trimmed fields and shows the receipt.</summary>
    [Fact]
    public async Task SubmitAsync_ValidBooking_ShowsTheReceipt()
    {
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        _bookings.CreatePublicAsync(Arg.Any<CreateBookingRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(9)));
        var viewModel = await AtGuestStepAsync();
        viewModel.PassportNumber = " P1 ";

        await viewModel.SubmitAsync(Token);

        Assert.Equal(BookingFlowStep.Receipt, viewModel.CurrentStep);
        Assert.Equal(9, viewModel.Confirmation!.BookingId);
        await _bookings.Received(1).CreatePublicAsync(
            new CreateBookingRequest(
                Today.AddDays(1),
                Today.AddDays(2),
                1,
                null,
                new GuestFields("Rita", "Hansen", "rita@example.com", "+4512345678", "Denmark", "P1")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Error handling: a room taken meanwhile drops the room, searches again and keeps the reason.</summary>
    [Fact]
    public async Task SubmitAsync_RoomTakenMeanwhile_SearchesAgainAndKeepsTheReason()
    {
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        _bookings.CreatePublicAsync(Arg.Any<CreateBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Fail<BookingDetailsDto>(ErrorCodes.Booking.RoomNotAvailable));
        var viewModel = await AtGuestStepAsync();

        await viewModel.SubmitAsync(Token);

        Assert.Null(viewModel.SelectedRoom);
        Assert.Equal(BookingFlowStep.Room, viewModel.CurrentStep);
        Assert.Equal([Text(ErrorCodes.Booking.RoomNotAvailable)], viewModel.Errors);
    }

    /// <summary>Error handling: a refusal that names no room keeps the choice and shows the reason; a failed new search keeps its own error.</summary>
    [Fact]
    public async Task SubmitAsync_OtherRefusalOrFailedSearch_ShowsTheRightReason()
    {
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        _bookings.CreatePublicAsync(Arg.Any<CreateBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Fail<BookingDetailsDto>(ErrorCodes.Booking.ConcurrencyConflict), Fail<BookingDetailsDto>(ErrorCodes.Booking.RoomNotBookable));
        var viewModel = await AtGuestStepAsync();

        await viewModel.SubmitAsync(Token);
        Assert.NotNull(viewModel.SelectedRoom);
        Assert.Equal([Text(ErrorCodes.Booking.ConcurrencyConflict)], viewModel.Errors);

        _bookings.GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Fail<IReadOnlyList<AvailableRoomDto>>(ErrorCodes.Booking.StartDateInPast));
        await viewModel.SubmitAsync(Token);
        Assert.Equal([Text(ErrorCodes.Booking.StartDateInPast)], viewModel.Errors);
    }

    /// <summary>Negative: without valid dates or a room, submitting goes back to the step that is missing.</summary>
    [Fact]
    public async Task SubmitAsync_MissingDatesOrRoom_GoesBack()
    {
        var viewModel = Create();
        await viewModel.SubmitAsync(Token);
        Assert.Equal(BookingFlowStep.Dates, viewModel.CurrentStep);

        RoomsAre(FreeRoom(1));
        await viewModel.FindRoomsAsync(Token);
        await viewModel.SubmitAsync(Token);
        Assert.Equal(BookingFlowStep.Room, viewModel.CurrentStep);

        viewModel.CheckInDate = null;
        await viewModel.SubmitAsync(Token);
        Assert.Equal(BookingFlowStep.Dates, viewModel.CurrentStep);
        Assert.Equal(Text(ErrorCodes.Booking.StartDateRequired), viewModel.CheckInError);
    }

    #endregion

    #region Steps

    /// <summary>Concurrency: while a search runs, every other step is ignored.</summary>
    [Fact]
    public async Task Steps_WhileBusy_AreIgnored()
    {
        var pending = new TaskCompletionSource<Result<IReadOnlyList<AvailableRoomDto>>>();
        _bookings.GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(pending.Task);
        var viewModel = Create();

        var search = viewModel.FindRoomsAsync(Token);
        Assert.False(viewModel.CanFindRooms);
        await viewModel.FindRoomsAsync(Token);
        viewModel.SelectRoom(1);
        viewModel.BackToDates();
        viewModel.BackToRooms();
        await viewModel.SubmitAsync(Token);
        viewModel.StartOver();
        pending.SetResult(Result<IReadOnlyList<AvailableRoomDto>>.Success([FreeRoom(1)]));
        await search;

        Assert.Equal(BookingFlowStep.Room, viewModel.CurrentStep);
        await _bookings.Received(1).GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region StartOver

    /// <summary>State transition: starting over clears everything and goes back to the dates.</summary>
    [Fact]
    public async Task StartOver_AfterReceipt_ClearsTheFlow()
    {
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        _bookings.CreatePublicAsync(Arg.Any<CreateBookingRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(9)));
        var viewModel = await AtGuestStepAsync();
        await viewModel.SubmitAsync(Token);
        _clock.Today = Today.AddDays(5);

        viewModel.StartOver();

        Assert.Equal(BookingFlowStep.Dates, viewModel.CurrentStep);
        Assert.Null(viewModel.Confirmation);
        Assert.Null(viewModel.SelectedRoom);
        Assert.Empty(viewModel.AvailableRooms);
        Assert.Equal(string.Empty, viewModel.FirstName);
        Assert.Same(GuestFieldErrors.None, viewModel.GuestErrors);
        Assert.Equal(Today.AddDays(6), viewModel.CheckInDate);
    }

    #endregion
}

/// <summary>The receipt page, the guest field errors, the booking number and the public dates (MIL-054 task 3).</summary>
public sealed class BookingReceiptTests
{
    private readonly IBookingService _bookings = Substitute.For<IBookingService>();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private BookingReceiptViewModel Create() => new(new PassThroughRunner<IBookingService>(_bookings));

    /// <summary>Negative: the runner is required.</summary>
    [Fact]
    public void Constructor_MissingRunner_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new BookingReceiptViewModel(null!));
    }

    /// <summary>Positive: a readable number loads the booking.</summary>
    [Fact]
    public async Task LoadAsync_KnownNumber_ShowsTheBooking()
    {
        _bookings.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(42)));
        var viewModel = Create();

        await viewModel.LoadAsync("FLZ-42", Token);

        Assert.True(viewModel.IsLoaded);
        Assert.Equal(42, viewModel.Details!.BookingId);
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Negative: an unreadable number is not looked up and says not found.</summary>
    [Fact]
    public async Task LoadAsync_UnreadableNumber_SaysNotFoundWithoutALookup()
    {
        var viewModel = Create();

        await viewModel.LoadAsync("abc", Token);

        Assert.Equal([Text(ErrorCodes.Booking.NotFound)], viewModel.Errors);
        Assert.Empty(_bookings.ReceivedCalls());
    }

    /// <summary>Error handling: a refused lookup shows its text.</summary>
    [Fact]
    public async Task LoadAsync_Refused_ShowsTheText()
    {
        _bookings.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(Fail<BookingDetailsDto>(ErrorCodes.Booking.NotFound));
        var viewModel = Create();

        await viewModel.LoadAsync("42", Token);

        Assert.Null(viewModel.Details);
        Assert.Equal([Text(ErrorCodes.Booking.NotFound)], viewModel.Errors);
    }

    /// <summary>Concurrency: a second load while one runs is ignored.</summary>
    [Fact]
    public async Task LoadAsync_WhileLoading_IsIgnored()
    {
        var pending = new TaskCompletionSource<Result<BookingDetailsDto>>();
        _bookings.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(pending.Task);
        var viewModel = Create();

        var load = viewModel.LoadAsync("42", Token);
        Assert.Empty(viewModel.Errors);
        await viewModel.LoadAsync("42", Token);
        pending.SetResult(Result<BookingDetailsDto>.Success(BookingDetails(42)));
        await load;

        await _bookings.Received(1).GetByIdAsync(42, Arg.Any<CancellationToken>());
    }

    /// <summary>Boundary and equivalence: the booking number with or without its prefix, and every way it can be unreadable.</summary>
    [Theory]
    [InlineData("FLZ-0042", true, 42)]
    [InlineData(" flz-7 ", true, 7)]
    [InlineData("123456789", true, 123456789)]
    [InlineData("1234567890", false, 0)]
    [InlineData("FLZ-", false, 0)]
    [InlineData("0", false, 0)]
    [InlineData("-5", false, 0)]
    [InlineData("4 2", false, 0)]
    [InlineData("", false, 0)]
    [InlineData(null, false, 0)]
    public void TryParseId_Text_ReadsOnlyAPositiveNumber(string? text, bool expected, int expectedId)
    {
        Assert.Equal(expected, BookingNumberText.TryParseId(text, out var id));
        Assert.Equal(expectedId, id);
    }

    /// <summary>Equivalence: each guest error lands on its field once; the rest are other errors; a success has none.</summary>
    [Fact]
    public void FromResult_EveryGuestError_LandsOnItsField()
    {
        string[] codes =
        [
            ErrorCodes.Guest.FirstNameRequired, ErrorCodes.Guest.FirstNameTooLong,
            ErrorCodes.Guest.LastNameRequired, ErrorCodes.Guest.LastNameTooLong,
            ErrorCodes.Guest.EmailRequired, ErrorCodes.Guest.EmailInvalid, ErrorCodes.Guest.EmailTooLong,
            ErrorCodes.Guest.PhoneNumberRequired, ErrorCodes.Guest.PhoneNumberTooLong,
            ErrorCodes.Guest.CountryRequired, ErrorCodes.Guest.CountryTooLong,
            ErrorCodes.Guest.PassportNumberTooLong,
            ErrorCodes.Booking.NotFound
        ];

        var errors = GuestFieldErrors.FromResult(Result.FromCodes(codes));

        Assert.Equal(Text(ErrorCodes.Guest.FirstNameRequired), errors.FirstName);
        Assert.Equal(Text(ErrorCodes.Guest.LastNameRequired), errors.LastName);
        Assert.Equal(Text(ErrorCodes.Guest.EmailRequired), errors.Email);
        Assert.Equal(Text(ErrorCodes.Guest.PhoneNumberRequired), errors.PhoneNumber);
        Assert.Equal(Text(ErrorCodes.Guest.CountryRequired), errors.Country);
        Assert.Equal(Text(ErrorCodes.Guest.PassportNumberTooLong), errors.PassportNumber);
        Assert.Equal([Text(ErrorCodes.Booking.NotFound)], errors.Other);
        Assert.Same(GuestFieldErrors.None, GuestFieldErrors.FromResult(Result.Success()));
        Assert.Throws<ArgumentNullException>(() => GuestFieldErrors.FromResult(null!));
    }

    /// <summary>Boundary: the public dates name the month, and one night is singular.</summary>
    [Fact]
    public void PublicDateText_Dates_AreWrittenOut()
    {
        Assert.Equal("1 January 2026", PublicDateText.Long(new DateOnly(2026, 1, 1)));
        Assert.Equal("31 December 2026", PublicDateText.Long(new DateOnly(2026, 12, 31)));
        Assert.Equal("1 night", PublicDateText.Nights(1));
        Assert.Equal("2 nights", PublicDateText.Nights(2));
    }

    /// <summary>Equivalence: the status names used on the receipt are words, not enum names.</summary>
    [Fact]
    public void BookingStatus_EveryValue_HasAName()
    {
        Assert.All(Enum.GetValues<BookingStatus>(), status => Assert.False(string.IsNullOrWhiteSpace(status.ToString())));
    }
}
