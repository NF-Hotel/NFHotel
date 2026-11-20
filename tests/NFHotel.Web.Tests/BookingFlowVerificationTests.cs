using System.Globalization;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;
using NFHotel.Web.Common;
using NFHotel.Web.Features.Booking;
using NFHotel.Web.Security;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>
/// The booking flow with the verification by email (MIL-088, ADR-0052): the flow asks the mode once, asks for the booking
/// instead of making it, ends in the inbox step with the email and the time of the link, and asks again with the same
/// answers. With the verification off the flow books at once, as <see cref="BookingFlowViewModelTests"/> shows.
/// </summary>
public sealed class BookingFlowVerificationTests
{
    #region Setup

    private static readonly DateTimeOffset LinkLapses = new(2026, 3, 10, 12, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset NewLinkLapses = new(2026, 3, 10, 12, 45, 0, TimeSpan.Zero);

    private readonly IBookingService _bookings = Substitute.For<IBookingService>();
    private readonly IGuestService _guests = Substitute.For<IGuestService>();
    private readonly IGuestAccessService _access = Substitute.For<IGuestAccessService>();
    private readonly IBookingRequestService _requests = Substitute.For<IBookingRequestService>();
    private readonly FakeClock _clock = new() { Today = Today };
    private readonly GuestSessionTickets _tickets;

    public BookingFlowVerificationTests()
    {
        _tickets = new GuestSessionTickets(_clock);
        _bookings.GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(List(FreeRoom(1), FreeRoom(2)));
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        _requests.AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Ok(new BookingRequestReceiptDto(LinkLapses)), Ok(new BookingRequestReceiptDto(NewLinkLapses)));
    }

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private BookingFlowViewModel Create() =>
        new(
            new PassThroughRunner<IBookingService>(_bookings),
            new PassThroughRunner<IGuestService>(_guests),
            new PassThroughRunner<IGuestAccessService>(_access),
            _tickets,
            _clock,
            new PassThroughRunner<IBookingRequestService>(_requests));

    private void ModeIs(bool verificationRequired) =>
        _requests.GetModeAsync(Arg.Any<CancellationToken>()).Returns(Ok(new PublicBookingModeDto(verificationRequired)));

    /// <summary>A flow in the given mode at the guest step, with the details typed as a guest types them.</summary>
    private async Task<BookingFlowViewModel> AtGuestStepAsync(bool verificationRequired = true)
    {
        ModeIs(verificationRequired);
        BookingFlowViewModel viewModel = Create();
        await viewModel.LoadModeAsync(Token);
        await viewModel.FindRoomsAsync(Token);
        viewModel.SelectRoomType(RoomSize.Double);
        viewModel.FirstName = " Rita ";
        viewModel.LastName = "Hansen";
        viewModel.Email = " rita@example.com ";
        viewModel.PhoneNumber = "+4512345678";
        viewModel.Country = "Denmark";
        return viewModel;
    }

    /// <summary>A flow with the verification on, in the inbox step after the first request.</summary>
    private async Task<BookingFlowViewModel> InTheInboxAsync()
    {
        BookingFlowViewModel viewModel = await AtGuestStepAsync();
        await viewModel.SubmitAsync(Token);
        Assert.Equal(BookingFlowStep.Inbox, viewModel.CurrentStep);
        return viewModel;
    }

    /// <summary>The request of the flow at the guest step: trimmed, as a booking at once is.</summary>
    private static CreatePublicBookingRequest Expected() => new(
        Today.AddDays(1),
        Today.AddDays(2),
        RoomSize.Double,
        1,
        new GuestFields("Rita", "Hansen", "rita@example.com", "+4512345678", "Denmark", null));

    #endregion

    #region The mode

    /// <summary>Positive (ADR-0052 decision 8): the flow asks the API which way holds once, however often the page asks.</summary>
    [Fact]
    public async Task LoadModeAsync_CalledTwice_AsksTheApiOnce()
    {
        // Arrange
        ModeIs(true);
        BookingFlowViewModel viewModel = Create();

        // Act
        await viewModel.LoadModeAsync(Token);
        await viewModel.LoadModeAsync(Token);

        // Assert
        await _requests.Received(1).GetModeAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Equivalence: the answer of the API sets the mode, and with it the last step and its name.</summary>
    /// <param name="verificationRequired">The mode the API answers.</param>
    /// <param name="last">The last step of the flow.</param>
    /// <param name="name">The name of the last step in the step indicator.</param>
    [Theory]
    [InlineData(true, BookingFlowStep.Inbox, "Check your inbox")]
    [InlineData(false, BookingFlowStep.Receipt, "Confirmation")]
    public async Task LoadModeAsync_Answer_SetsTheLastStep(bool verificationRequired, BookingFlowStep last, string name)
    {
        // Arrange
        ModeIs(verificationRequired);
        BookingFlowViewModel viewModel = Create();

        // Act
        await viewModel.LoadModeAsync(Token);

        // Assert
        Assert.Equal(verificationRequired, viewModel.IsVerificationRequired);
        Assert.Equal(last, viewModel.FinalStep);
        Assert.Equal(name, viewModel.FinalStepText);
    }

    /// <summary>
    /// Error handling: an API that cannot say leaves the flow booking at once. The API refuses that when the verification
    /// is on, and the page asks for a reload (<see cref="SubmitAsync_BookingAtOnceRefused_AsksForAReload"/>).
    /// </summary>
    [Fact]
    public async Task LoadModeAsync_ApiCannotSay_BooksAtOnce()
    {
        // Arrange
        _requests.GetModeAsync(Arg.Any<CancellationToken>()).Returns(Fail<PublicBookingModeDto>(ErrorCodes.Api.Unavailable));
        BookingFlowViewModel viewModel = Create();

        // Act
        await viewModel.LoadModeAsync(Token);

        // Assert
        Assert.False(viewModel.IsVerificationRequired);
    }

    /// <summary>Edge case: a flow built without the runner of the requests asks nothing and books at once, as before MIL-088.</summary>
    [Fact]
    public async Task LoadModeAsync_WithoutTheRunner_BooksAtOnce()
    {
        // Arrange
        BookingFlowViewModel viewModel = new(
            new PassThroughRunner<IBookingService>(_bookings),
            new PassThroughRunner<IGuestService>(_guests),
            new PassThroughRunner<IGuestAccessService>(_access),
            _tickets,
            _clock);

        // Act
        await viewModel.LoadModeAsync(Token);

        // Assert
        Assert.False(viewModel.IsVerificationRequired);
        Assert.Empty(_requests.ReceivedCalls());
    }

    #endregion

    #region Ask

    /// <summary>
    /// Positive (ADR-0052 decision 1): with the verification on, the last button asks with the same answers as a booking
    /// at once, trimmed, and books nothing.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_VerificationOn_AsksWithTheSameAnswersAndBooksNothing()
    {
        // Arrange
        BookingFlowViewModel viewModel = await AtGuestStepAsync();

        // Act
        await viewModel.SubmitAsync(Token);

        // Assert
        await _requests.Received(1).AskAsync(Expected(), Arg.Any<CancellationToken>());
        await _access.DidNotReceiveWithAnyArgs().BookAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Positive: the request taken, the flow ends in the inbox step with the email the link went to and the time the link
    /// lapses, in UTC as the mail writes it. There is no receipt and no session: nothing is booked yet.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_VerificationOn_EndsInTheInboxWithTheEmailAndTheTime()
    {
        // Arrange
        BookingFlowViewModel viewModel = await AtGuestStepAsync();

        // Act
        await viewModel.SubmitAsync(Token);

        // Assert
        Assert.Equal(BookingFlowStep.Inbox, viewModel.CurrentStep);
        Assert.Equal("We've sent an email with a link to rita@example.com.", viewModel.InboxText);
        Assert.Equal(
            "Open the link and press the button on its page: your room is booked only then. "
                + "The link works once, until 12:30 UTC on 10 March 2026.",
            viewModel.LinkText);
        Assert.Null(viewModel.Confirmation);
        Assert.Null(viewModel.SessionTicket);
        Assert.Null(viewModel.Notice);
    }

    /// <summary>Equivalence: with the verification off, the last button books at once and asks nothing.</summary>
    [Fact]
    public async Task SubmitAsync_VerificationOff_BooksAtOnceAndAsksNothing()
    {
        // Arrange
        _access.BookAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Ok(new PublicBookingDto(Receipt(9), new GuestSessionDto("guest-token-9", LinkLapses))));
        BookingFlowViewModel viewModel = await AtGuestStepAsync(verificationRequired: false);

        // Act
        await viewModel.SubmitAsync(Token);

        // Assert
        Assert.Equal(BookingFlowStep.Receipt, viewModel.CurrentStep);
        await _access.Received(1).BookAsync(Expected(), Arg.Any<CancellationToken>());
        await _requests.DidNotReceiveWithAnyArgs().AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: invalid details are shown at their fields, and nothing is asked for.</summary>
    [Fact]
    public async Task SubmitAsync_InvalidGuestWithVerificationOn_AsksNothing()
    {
        // Arrange
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Failure(ErrorCodes.Guest.EmailInvalid));
        BookingFlowViewModel viewModel = await AtGuestStepAsync();

        // Act
        await viewModel.SubmitAsync(Token);

        // Assert
        Assert.Equal(Text(ErrorCodes.Guest.EmailInvalid), viewModel.GuestErrors.Email);
        await _requests.DidNotReceiveWithAnyArgs().AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Error handling (ADR-0052 decision 6, ADR-0053, ADR-0051 decision 2): a request that a limit or the mail refuses
    /// stays at the details with everything typed and the text of its code, so the same form can be sent again.
    /// </summary>
    /// <param name="code">The code of the refusal.</param>
    [Theory]
    [InlineData(ErrorCodes.Booking.TooManyMails)]
    [InlineData(ErrorCodes.Booking.TooManyRequests)]
    [InlineData(ErrorCodes.Booking.MailNotSent)]
    [InlineData(ErrorCodes.Booking.MailPaused)]
    public async Task SubmitAsync_RequestRefused_StaysAtTheDetailsWithTheText(string code)
    {
        // Arrange
        _requests.AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Fail<BookingRequestReceiptDto>(code));
        BookingFlowViewModel viewModel = await AtGuestStepAsync();

        // Act
        await viewModel.SubmitAsync(Token);

        // Assert
        Assert.Equal(BookingFlowStep.Guest, viewModel.CurrentStep);
        Assert.Equal([Text(code)], viewModel.Errors);
        Assert.Equal(" rita@example.com ", viewModel.Email);
        Assert.True(viewModel.CanSubmit);
    }

    /// <summary>Error handling: a room type gone at the request drops the choice, searches again and keeps the reason.</summary>
    [Fact]
    public async Task SubmitAsync_RoomTypeGoneAtTheRequest_SearchesAgainAndKeepsTheReason()
    {
        // Arrange
        _requests.AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Fail<BookingRequestReceiptDto>(ErrorCodes.Booking.RoomTypeNotAvailable));
        BookingFlowViewModel viewModel = await AtGuestStepAsync();

        // Act
        await viewModel.SubmitAsync(Token);

        // Assert
        Assert.Equal(BookingFlowStep.Room, viewModel.CurrentStep);
        Assert.Null(viewModel.SelectedRoomType);
        Assert.Equal([Text(ErrorCodes.Booking.RoomTypeNotAvailable)], viewModel.Errors);
    }

    /// <summary>
    /// Error handling (ADR-0052 decision 8): with the verification switched off after the page asked the mode, the API
    /// answers the request with not found; the guest is asked to reload the page, not told that a booking is missing.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_VerificationSwitchedOffMeanwhile_AsksForAReload()
    {
        // Arrange
        _requests.AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Fail<BookingRequestReceiptDto>(ErrorCodes.Booking.NotFound));
        BookingFlowViewModel viewModel = await AtGuestStepAsync();

        // Act
        await viewModel.SubmitAsync(Token);

        // Assert
        Assert.Equal([Text(ErrorCodes.Booking.VerificationRequired)], viewModel.Errors);
    }

    /// <summary>
    /// Error handling (ADR-0052 decision 8): with the verification switched on after the page asked the mode, or when the
    /// API could not say, the booking at once is refused; the guest is asked to reload the page, which asks again.
    /// </summary>
    [Fact]
    public async Task SubmitAsync_BookingAtOnceRefused_AsksForAReload()
    {
        // Arrange
        _access.BookAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Fail<PublicBookingDto>(ErrorCodes.Booking.VerificationRequired));
        BookingFlowViewModel viewModel = await AtGuestStepAsync(verificationRequired: false);

        // Act
        await viewModel.SubmitAsync(Token);

        // Assert
        Assert.Equal(BookingFlowStep.Guest, viewModel.CurrentStep);
        Assert.Equal(["The way to book has changed. Please reload the page and try again."], viewModel.Errors);
    }

    #endregion

    #region Send again

    /// <summary>Positive (ADR-0052 decision 6): "Send again" asks again with the same answers.</summary>
    [Fact]
    public async Task SendAgainAsync_FromTheInbox_AsksAgainWithTheSameAnswers()
    {
        // Arrange
        BookingFlowViewModel viewModel = await InTheInboxAsync();

        // Act
        await viewModel.SendAgainAsync(Token);

        // Assert
        await _requests.Received(2).AskAsync(Expected(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// State transition: a new request taken, the inbox step shows the time of the new link and says that the link of the
    /// earlier mail no longer works.
    /// </summary>
    [Fact]
    public async Task SendAgainAsync_Taken_ShowsTheNewTimeAndThatTheEarlierLinkStopped()
    {
        // Arrange
        BookingFlowViewModel viewModel = await InTheInboxAsync();

        // Act
        await viewModel.SendAgainAsync(Token);

        // Assert
        Assert.Equal(BookingFlowStep.Inbox, viewModel.CurrentStep);
        Assert.Equal(NewLinkLapses, viewModel.LinkExpiresAt);
        Assert.Equal("We've sent you a new link. The link in the earlier email no longer works.", viewModel.Notice);
    }

    /// <summary>
    /// Error handling (ADR-0052 decision 6): "Send again" refused by the limit of the mails stays in the inbox step with the
    /// text that sends the guest to the newest mail, and drops the notice of the send before it.
    /// </summary>
    [Fact]
    public async Task SendAgainAsync_RefusedByTheLimitOfTheMails_StaysInTheInboxWithTheText()
    {
        // Arrange
        _requests.AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(
                Ok(new BookingRequestReceiptDto(LinkLapses)),
                Ok(new BookingRequestReceiptDto(NewLinkLapses)),
                Fail<BookingRequestReceiptDto>(ErrorCodes.Booking.TooManyMails));
        BookingFlowViewModel viewModel = await InTheInboxAsync();
        await viewModel.SendAgainAsync(Token);

        // Act
        await viewModel.SendAgainAsync(Token);

        // Assert
        Assert.Equal(BookingFlowStep.Inbox, viewModel.CurrentStep);
        Assert.Equal([Text(ErrorCodes.Booking.TooManyMails)], viewModel.Errors);
        Assert.Null(viewModel.Notice);
    }

    /// <summary>Error handling: a refusal that names a field takes the guest back to the details, where the field is.</summary>
    [Fact]
    public async Task SendAgainAsync_FieldRefused_GoesBackToTheDetails()
    {
        // Arrange
        _requests.AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Ok(new BookingRequestReceiptDto(LinkLapses)), Fail<BookingRequestReceiptDto>(ErrorCodes.Guest.EmailInvalid));
        BookingFlowViewModel viewModel = await InTheInboxAsync();

        // Act
        await viewModel.SendAgainAsync(Token);

        // Assert
        Assert.Equal(BookingFlowStep.Guest, viewModel.CurrentStep);
        Assert.Equal(Text(ErrorCodes.Guest.EmailInvalid), viewModel.GuestErrors.Email);
    }

    /// <summary>Negative: outside the inbox step "Send again" asks nothing.</summary>
    [Fact]
    public async Task SendAgainAsync_OutsideTheInbox_AsksNothing()
    {
        // Arrange
        BookingFlowViewModel viewModel = await AtGuestStepAsync();

        // Act
        await viewModel.SendAgainAsync(Token);

        // Assert
        Assert.False(viewModel.CanSendAgain);
        await _requests.DidNotReceiveWithAnyArgs().AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Boundary (BR-C-04): once the day of arrival has passed, "Send again" goes back to the dates and asks nothing.</summary>
    [Fact]
    public async Task SendAgainAsync_ArrivalPassed_GoesBackToTheDates()
    {
        // Arrange
        BookingFlowViewModel viewModel = await InTheInboxAsync();
        _clock.Today = Today.AddDays(2);

        // Act
        await viewModel.SendAgainAsync(Token);

        // Assert
        Assert.Equal(BookingFlowStep.Dates, viewModel.CurrentStep);
        await _requests.Received(1).AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>Concurrency: while a request is on its way, a second send, the way back and a new start are ignored.</summary>
    [Fact]
    public async Task SendAgainAsync_WhileAsking_OtherStepsAreIgnored()
    {
        // Arrange
        BookingFlowViewModel viewModel = await InTheInboxAsync();
        TaskCompletionSource<Result<BookingRequestReceiptDto>> pending = new();
        _requests.AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>()).Returns(pending.Task);

        // Act
        Task sending = viewModel.SendAgainAsync(Token);
        await viewModel.SendAgainAsync(Token);
        viewModel.BackToDetails();
        viewModel.StartOver();
        pending.SetResult(Result<BookingRequestReceiptDto>.Success(new BookingRequestReceiptDto(NewLinkLapses)));
        await sending;

        // Assert
        Assert.Equal(BookingFlowStep.Inbox, viewModel.CurrentStep);
        await _requests.Received(2).AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Back and start over

    /// <summary>State transition: "Change your details" goes from the inbox step back to the details with everything typed.</summary>
    [Fact]
    public async Task BackToDetails_FromTheInbox_KeepsEverythingTyped()
    {
        // Arrange
        BookingFlowViewModel viewModel = await InTheInboxAsync();

        // Act
        viewModel.BackToDetails();

        // Assert
        Assert.Equal(BookingFlowStep.Guest, viewModel.CurrentStep);
        Assert.Equal(" rita@example.com ", viewModel.Email);
        Assert.Equal(RoomSize.Double, viewModel.SelectedRoomType);
    }

    /// <summary>State transition: starting over from the inbox step clears the request and keeps the mode of the API.</summary>
    [Fact]
    public async Task StartOver_FromTheInbox_ClearsTheRequestAndKeepsTheMode()
    {
        // Arrange
        BookingFlowViewModel viewModel = await InTheInboxAsync();
        await viewModel.SendAgainAsync(Token);

        // Act
        viewModel.StartOver();

        // Assert
        Assert.Equal(BookingFlowStep.Dates, viewModel.CurrentStep);
        Assert.Null(viewModel.SentTo);
        Assert.Null(viewModel.LinkExpiresAt);
        Assert.Null(viewModel.Notice);
        Assert.Equal(string.Empty, viewModel.InboxText);
        Assert.True(viewModel.IsVerificationRequired);
    }

    #endregion

    #region The hint at the email

    /// <summary>
    /// Equivalence: the hint at the email field says what the email is for in each mode, the link that books or the
    /// reference that finds the booking again, and never a mail that the mode does not send.
    /// </summary>
    /// <param name="verificationRequired">The mode the API answers.</param>
    /// <param name="hint">The hint at the email field.</param>
    [Theory]
    [InlineData(true, "We email you a link here. Your room is booked only when you use it.")]
    [InlineData(false, "With your booking reference, it finds your booking again.")]
    public async Task EmailHint_Mode_SaysWhatTheEmailIsFor(bool verificationRequired, string hint)
    {
        // Arrange
        ModeIs(verificationRequired);
        BookingFlowViewModel viewModel = Create();

        // Act
        await viewModel.LoadModeAsync(Token);

        // Assert
        Assert.Equal(hint, viewModel.EmailHint);
    }

    #endregion
}

/// <summary>The texts of the booking verified by email (MIL-088, ADR-0052 and ADR-0053).</summary>
public sealed class BookingRequestTextTests
{
    /// <summary>Equivalence: each code of the verification has a text of its own, never the neutral one.</summary>
    [Fact]
    public void Describe_CodesOfTheVerification_HaveTextsOfTheirOwn()
    {
        // Arrange
        string[] codes =
        [
            ErrorCodes.Booking.TooManyMails,
            ErrorCodes.Booking.MailNotSent,
            ErrorCodes.Booking.MailPaused,
            ErrorCodes.Booking.VerificationLinkInvalid,
            ErrorCodes.Booking.VerificationRequired,
        ];

        // Act
        string[] texts = codes.Select(Text).ToArray();

        // Assert
        Assert.DoesNotContain(ErrorMessages.Unexpected, texts);
        Assert.Equal(texts.Length, texts.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// Positive (ADR-0052 decision 6): the limit of the mails sends the guest to the newest mail and asks for no wait, so
    /// the text names no time.
    /// </summary>
    [Fact]
    public void Describe_TooManyMails_SendsToTheNewestMailWithoutAWait()
    {
        // Act
        string text = Text(ErrorCodes.Booking.TooManyMails);

        // Assert
        Assert.Contains("newest email", text, StringComparison.Ordinal);
        Assert.DoesNotContain("minute", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(text, char.IsDigit);
    }

    /// <summary>Positive (ADR-0053 decision 6): requests that are paused offer a call to the hotel as the other way.</summary>
    [Fact]
    public void Describe_MailPaused_OffersACallToTheHotel()
    {
        // Act
        string text = Text(ErrorCodes.Booking.MailPaused);

        // Assert
        Assert.Contains("call the hotel", text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Boundary: a moment is written in UTC with its date, also when its own offset puts it on another day, and on the
    /// last minute of a year.
    /// </summary>
    /// <param name="moment">The moment, with its offset.</param>
    /// <param name="text">The text of the page.</param>
    [Theory]
    [InlineData("2026-01-01T12:30:00+00:00", "12:30 UTC on 1 January 2026")]
    [InlineData("2026-03-11T00:30:00+02:00", "22:30 UTC on 10 March 2026")]
    [InlineData("2026-12-31T23:59:00+00:00", "23:59 UTC on 31 December 2026")]
    public void TimeUtc_Moment_IsWrittenInUtc(string moment, string text)
    {
        // Act
        string written = PublicDateText.TimeUtc(DateTimeOffset.Parse(moment, CultureInfo.InvariantCulture));

        // Assert
        Assert.Equal(text, written);
    }
}
