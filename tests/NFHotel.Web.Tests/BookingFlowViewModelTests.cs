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

/// <summary>The public booking flow of a guest: dates, room type, guest, receipt (MIL-054 task 3).</summary>
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
        viewModel.SelectRoomType(RoomSize.Double);
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

    /// <summary>Edge: a room type chosen for earlier dates is kept while a room of it is free, and dropped when none is.</summary>
    [Fact]
    public async Task FindRoomsAsync_ChosenTypeNoLongerFree_DropsIt()
    {
        BookingFlowViewModel viewModel = await AtGuestStepAsync();
        RoomsAre(FreeRoom(2));
        await viewModel.FindRoomsAsync(Token);
        Assert.Equal(RoomSize.Double, viewModel.SelectedRoomType);

        RoomsAre(new AvailableRoomDto(3, "301", 3, RoomSize.Suite, 4));
        await viewModel.FindRoomsAsync(Token);

        Assert.Null(viewModel.SelectedRoomType);
    }

    #endregion

    #region SelectRoomType

    /// <summary>Positive: choosing a room type moves on to the guest and sums up the choice without a room number.</summary>
    [Fact]
    public async Task SelectRoomType_FreeType_MovesToTheGuest()
    {
        BookingFlowViewModel viewModel = await AtGuestStepAsync();

        Assert.Equal(BookingFlowStep.Guest, viewModel.CurrentStep);
        Assert.True(viewModel.CanSubmit);
        Assert.Equal(
            "Double room · 11 March 2026 – 12 March 2026 · 1 night · 1 guest · Payment on arrival",
            viewModel.SelectionSummary);
    }

    /// <summary>Negative: a type with no free room is ignored, and invalid dates send the guest back.</summary>
    [Fact]
    public async Task SelectRoomType_TypeNotFreeOrInvalidDates_IsIgnoredOrSentBack()
    {
        RoomsAre(FreeRoom(1));
        BookingFlowViewModel viewModel = Create();
        await viewModel.FindRoomsAsync(Token);

        viewModel.SelectRoomType(RoomSize.Suite);
        Assert.Equal(BookingFlowStep.Room, viewModel.CurrentStep);

        viewModel.CheckInDate = null;
        viewModel.SelectRoomType(RoomSize.Double);
        Assert.Equal(BookingFlowStep.Dates, viewModel.CurrentStep);
        Assert.Null(viewModel.SelectedRoomType);
    }

    #endregion

    #region Describe

    /// <summary>Equivalence: a type is described by how many its biggest free room sleeps and how many rooms are free.</summary>
    [Fact]
    public void Describe_SleepsAndFree_ReadAsText()
    {
        Assert.Equal("sleeps 1", BookingFlowViewModel.DescribeSleeps(1));
        Assert.Equal("sleeps up to 3", BookingFlowViewModel.DescribeSleeps(3));
        Assert.Equal("Last room", BookingFlowViewModel.DescribeFree(1));
        Assert.Equal("4 rooms free", BookingFlowViewModel.DescribeFree(4));
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
        await _bookings.DidNotReceiveWithAnyArgs().CreatePublicAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Positive: a valid booking is sent as a new guest with trimmed fields and keeps only its receipt (MIL-079).</summary>
    [Fact]
    public async Task SubmitAsync_ValidBooking_ShowsTheReceipt()
    {
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        _bookings.CreatePublicAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(9)));
        var viewModel = await AtGuestStepAsync();
        viewModel.PassportNumber = " P1 ";

        await viewModel.SubmitAsync(Token);

        Assert.Equal(BookingFlowStep.Receipt, viewModel.CurrentStep);
        Assert.Equal(BookingReceiptDto.From(BookingDetails(9)), viewModel.Confirmation);
        await _bookings.Received(1).CreatePublicAsync(
            new CreatePublicBookingRequest(
                Today.AddDays(1),
                Today.AddDays(2),
                RoomSize.Double,
                1,
                new GuestFields("Rita", "Hansen", "rita@example.com", "+4512345678", "Denmark", "P1")),
            Arg.Any<CancellationToken>());
    }

    /// <summary>Error handling: a type taken meanwhile drops the choice, searches again and keeps the reason.</summary>
    [Theory]
    [InlineData(ErrorCodes.Booking.RoomNotAvailable)]
    [InlineData(ErrorCodes.Booking.RoomTypeNotAvailable)]
    public async Task SubmitAsync_TypeTakenMeanwhile_SearchesAgainAndKeepsTheReason(string code)
    {
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        _bookings.CreatePublicAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Fail<BookingDetailsDto>(code));
        BookingFlowViewModel viewModel = await AtGuestStepAsync();

        await viewModel.SubmitAsync(Token);

        Assert.Null(viewModel.SelectedRoomType);
        Assert.Equal(BookingFlowStep.Room, viewModel.CurrentStep);
        Assert.Equal([Text(code)], viewModel.Errors);
    }

    /// <summary>Error handling: a refusal that names no room keeps the choice and shows the reason; a failed new search keeps its own error.</summary>
    [Fact]
    public async Task SubmitAsync_OtherRefusalOrFailedSearch_ShowsTheRightReason()
    {
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        _bookings.CreatePublicAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Fail<BookingDetailsDto>(ErrorCodes.Booking.ConcurrencyConflict), Fail<BookingDetailsDto>(ErrorCodes.Booking.RoomNotBookable));
        var viewModel = await AtGuestStepAsync();

        await viewModel.SubmitAsync(Token);
        Assert.NotNull(viewModel.SelectedRoomType);
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
        viewModel.SelectRoomType(RoomSize.Double);
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
        _bookings.CreatePublicAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>()).Returns(Ok(BookingDetails(9)));
        var viewModel = await AtGuestStepAsync();
        await viewModel.SubmitAsync(Token);
        _clock.Today = Today.AddDays(5);

        viewModel.StartOver();

        Assert.Equal(BookingFlowStep.Dates, viewModel.CurrentStep);
        Assert.Null(viewModel.Confirmation);
        Assert.Null(viewModel.SelectedRoomType);
        Assert.Empty(viewModel.AvailableRooms);
        Assert.Equal(string.Empty, viewModel.FirstName);
        Assert.Same(GuestFieldErrors.None, viewModel.GuestErrors);
        Assert.Equal(Today.AddDays(6), viewModel.CheckInDate);
    }

    #endregion

    #region Guests and prefill

    /// <summary>Positive: the address of the page sets the dates and the party, and says the rooms can be fetched.</summary>
    [Fact]
    public void Prefill_ValidAddress_SetsDatesAndParty()
    {
        var viewModel = Create();

        var ready = viewModel.Prefill("2026-03-12", "2026-03-15", "3");

        Assert.True(ready);
        Assert.Equal(new DateOnly(2026, 3, 12), viewModel.CheckInDate);
        Assert.Equal(new DateOnly(2026, 3, 15), viewModel.CheckOutDate);
        Assert.Equal(3, viewModel.Guests);
        Assert.Equal("3 guests", viewModel.GuestsText);
    }

    /// <summary>Negative: an unreadable or missing date leaves the suggested dates alone; the party is still taken, within its bounds.</summary>
    [Fact]
    public void Prefill_BadAddress_KeepsTheSuggestedDates()
    {
        var viewModel = Create();

        Assert.False(viewModel.Prefill("12/03/2026", "2026-03-15", "9"));
        Assert.False(viewModel.Prefill(null, null, null));

        Assert.Equal(Today.AddDays(1), viewModel.CheckInDate);
        Assert.Equal(Today.AddDays(2), viewModel.CheckOutDate);
        Assert.Equal(BookingFlowViewModel.MaxGuests, viewModel.Guests);
    }

    /// <summary>Boundary: a past check-in from the address is read but is not valid, so the page stays on the dates.</summary>
    [Fact]
    public void Prefill_PastDate_IsNotReady()
    {
        var viewModel = Create();

        Assert.False(viewModel.Prefill("2026-03-01", "2026-03-04", "2"));
        Assert.Equal(new DateOnly(2026, 3, 1), viewModel.CheckInDate);
        Assert.False(viewModel.HasValidDates);
    }

    /// <summary>
    /// Negative: a party that does not fit an integer (the 44 digits of the DAST scan, one above the largest, one below
    /// the smallest), a word, a decimal, an empty value and white space are ignored; the dates are still taken (MIL-067).
    /// </summary>
    [Theory]
    [InlineData("99999999999999999999999999999999999999999999")]
    [InlineData("2147483648")]
    [InlineData("-2147483649")]
    [InlineData("two")]
    [InlineData("2.5")]
    [InlineData("")]
    [InlineData(" ")]
    public void Prefill_PartyThatDoesNotParse_KeepsTheDefaultAndTakesTheDates(string guests)
    {
        var viewModel = Create();

        var ready = viewModel.Prefill("2026-03-12", "2026-03-15", guests);

        Assert.True(ready);
        Assert.Equal(new DateOnly(2026, 3, 12), viewModel.CheckInDate);
        Assert.Equal(1, viewModel.Guests);
    }

    /// <summary>Boundary: a whole number of the address is kept between one and the most the field offers, the largest and smallest integers too.</summary>
    [Theory]
    [InlineData("1", 1)]
    [InlineData("0", 1)]
    [InlineData("-1", 1)]
    [InlineData("4", BookingFlowViewModel.MaxGuests)]
    [InlineData("5", BookingFlowViewModel.MaxGuests)]
    [InlineData("2147483647", BookingFlowViewModel.MaxGuests)]
    [InlineData("-2147483648", 1)]
    [InlineData(" 3 ", 3)]
    public void Prefill_WholeNumberParty_IsKeptWithinItsBounds(string guests, int expected)
    {
        var viewModel = Create();

        viewModel.Prefill(null, null, guests);

        Assert.Equal(expected, viewModel.Guests);
    }

    /// <summary>
    /// Equivalence: the free rooms are grouped into types, smallest type first, counting only the rooms that sleep the
    /// party; one guest, or "show all", counts every room.
    /// </summary>
    [Fact]
    public async Task VisibleRoomTypes_Party_GroupsAndFiltersByCapacity()
    {
        RoomsAre(
            new AvailableRoomDto(3, "301", 3, RoomSize.Suite, 4),
            FreeRoom(1),
            new AvailableRoomDto(2, "102", 1, RoomSize.Double, 3));
        BookingFlowViewModel viewModel = Create();
        await viewModel.FindRoomsAsync(Token);

        Assert.Equal(
            [new RoomTypeChoice(RoomSize.Double, 2, 3), new RoomTypeChoice(RoomSize.Suite, 1, 4)],
            viewModel.VisibleRoomTypes);

        viewModel.Guests = 3;
        Assert.Equal(
            [new RoomTypeChoice(RoomSize.Double, 1, 3), new RoomTypeChoice(RoomSize.Suite, 1, 4)],
            viewModel.VisibleRoomTypes);

        viewModel.Guests = 4;
        Assert.Equal([new RoomTypeChoice(RoomSize.Suite, 1, 4)], viewModel.VisibleRoomTypes);
        Assert.Equal("No free room sleeps 4", viewModel.NoRoomForGuestsText);

        viewModel.Guests = 0;
        Assert.Equal(1, viewModel.Guests);

        viewModel.Guests = 4;
        viewModel.ShowAllRooms();
        Assert.Equal(2, viewModel.VisibleRoomTypes.Count);

        viewModel.StartOver();
        Assert.Equal(1, viewModel.Guests);
    }

    #endregion
}

/// <summary>The receipt lookup (MIL-054 task 3, MIL-079), the guest field errors, the booking number and the public dates.</summary>
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

    /// <summary>Positive: the number and the email find the receipt; the number is sent as the key and the email as typed.</summary>
    [Fact]
    public async Task LookUpAsync_NumberAndEmail_ShowsTheReceipt()
    {
        var receipt = BookingReceiptDto.From(BookingDetails(42));
        _bookings.LookUpReceiptAsync(new BookingReceiptRequest(42, " Rita@Example.com"), Arg.Any<CancellationToken>()).Returns(Ok(receipt));
        var viewModel = Create();
        viewModel.Prefill("FLZ-42");
        viewModel.Email = " Rita@Example.com";

        await viewModel.LookUpAsync(Token);

        Assert.Equal(receipt, viewModel.Receipt);
        Assert.Empty(viewModel.Errors);
        Assert.False(viewModel.IsBusy);
    }

    /// <summary>Negative: an unreadable number is not looked up and gets the answer of a number that does not exist.</summary>
    [Fact]
    public async Task LookUpAsync_UnreadableNumber_SaysNotFoundWithoutALookup()
    {
        var viewModel = Create();
        viewModel.BookingNumber = "abc";
        viewModel.Email = "rita@example.com";

        await viewModel.LookUpAsync(Token);

        Assert.Null(viewModel.Receipt);
        Assert.Equal([Text(ErrorCodes.Booking.NotFound)], viewModel.Errors);
        Assert.Empty(_bookings.ReceivedCalls());
    }

    /// <summary>Error handling: each refusal of the lookup shows its own text and no receipt.</summary>
    /// <param name="code">The code of the refusal.</param>
    [Theory]
    [InlineData(ErrorCodes.Booking.NotFound)]
    [InlineData(ErrorCodes.Booking.ReceiptEmailRequired)]
    [InlineData(ErrorCodes.Booking.TooManyLookups)]
    public async Task LookUpAsync_Refused_ShowsTheTextOfTheRefusal(string code)
    {
        _bookings.LookUpReceiptAsync(Arg.Any<BookingReceiptRequest>(), Arg.Any<CancellationToken>()).Returns(Fail<BookingReceiptDto>(code));
        var viewModel = Create();
        viewModel.Prefill("42");

        await viewModel.LookUpAsync(Token);

        Assert.Null(viewModel.Receipt);
        Assert.Equal([Text(code)], viewModel.Errors);
    }

    /// <summary>State transition: a new number from the address fills in the form and forgets the last answer.</summary>
    [Fact]
    public async Task Prefill_AfterAnAnswer_FillsInTheNumberAndForgetsTheAnswer()
    {
        _bookings.LookUpReceiptAsync(Arg.Any<BookingReceiptRequest>(), Arg.Any<CancellationToken>()).Returns(Fail<BookingReceiptDto>(ErrorCodes.Booking.NotFound));
        var viewModel = Create();
        viewModel.Prefill("42");
        await viewModel.LookUpAsync(Token);

        viewModel.Prefill(" FLZ-7 ");
        var withNumber = viewModel.BookingNumber;
        viewModel.Prefill(null);

        Assert.Equal("FLZ-7", withNumber);
        Assert.Equal(string.Empty, viewModel.BookingNumber);
        Assert.Empty(viewModel.Errors);
        Assert.Null(viewModel.Receipt);
    }

    /// <summary>Concurrency: a second lookup while one runs is ignored.</summary>
    [Fact]
    public async Task LookUpAsync_WhileLookingUp_IsIgnored()
    {
        var pending = new TaskCompletionSource<Result<BookingReceiptDto>>();
        _bookings.LookUpReceiptAsync(Arg.Any<BookingReceiptRequest>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var viewModel = Create();
        viewModel.Prefill("42");
        viewModel.Email = "rita@example.com";

        var lookUp = viewModel.LookUpAsync(Token);
        Assert.True(viewModel.IsBusy);
        await viewModel.LookUpAsync(Token);
        pending.SetResult(Result<BookingReceiptDto>.Success(BookingReceiptDto.From(BookingDetails(42))));
        await lookUp;

        await _bookings.Received(1).LookUpReceiptAsync(Arg.Any<BookingReceiptRequest>(), Arg.Any<CancellationToken>());
        Assert.False(viewModel.IsBusy);
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
