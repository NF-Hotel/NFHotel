using Bunit;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.TestSupport;
using NFHotel.Web.Features.Booking;
using NFHotel.Web.Security;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>
/// The public booking page with the verification by email, rendered and clicked (MIL-088, ADR-0052): the page asks the
/// mode when it starts, its last button asks for the booking, and it ends in the inbox step instead of the receipt.
/// </summary>
public sealed class BookingInboxPageTests : IAsyncDisposable
{
    #region Setup

    private static readonly DateTimeOffset LinkLapses = new(2026, 3, 10, 12, 30, 0, TimeSpan.Zero);

    private readonly IBookingService _bookings = Substitute.For<IBookingService>();
    private readonly IGuestService _guests = Substitute.For<IGuestService>();
    private readonly IGuestAccessService _access = Substitute.For<IGuestAccessService>();
    private readonly IBookingRequestService _requests = Substitute.For<IBookingRequestService>();
    private readonly PageHost _host = new();

    public BookingInboxPageTests()
    {
        FakeClock clock = new() { Today = Today };
        _host.With(new BookingFlowViewModel(
            new PassThroughRunner<IBookingService>(_bookings),
            new PassThroughRunner<IGuestService>(_guests),
            new PassThroughRunner<IGuestAccessService>(_access),
            new GuestSessionTickets(clock),
            clock,
            new PassThroughRunner<IBookingRequestService>(_requests)));
        _guests.ValidateFields(Arg.Any<GuestFields>()).Returns(Result.Success());
        _bookings.GetAvailableRoomsAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(List(FreeRoom(1)));
        _requests.AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Ok(new BookingRequestReceiptDto(LinkLapses)));
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    private void ModeIs(bool verificationRequired) =>
        _requests.GetModeAsync(Arg.Any<CancellationToken>()).Returns(Ok(new PublicBookingModeDto(verificationRequired)));

    /// <summary>Renders the page in a mode and walks it to the details, typed in.</summary>
    private IRenderedComponent<Book> AtTheDetails(bool verificationRequired)
    {
        ModeIs(verificationRequired);
        IRenderedComponent<Book> page = _host.Render<Book>();
        page.Button("Find available rooms").Click();
        page.Button("Choose Double room").Click();
        page.Find("#first-name").Change("Rita");
        page.Find("#last-name").Change("Hansen");
        page.Find("#email").Change("rita@example.com");
        page.Find("#phone").Change("+4512345678");
        page.Find("#country").Change("Denmark");
        return page;
    }

    #endregion

    #region The inbox

    /// <summary>
    /// Positive (ADR-0052 decisions 1 and 3): with the verification on, the last button ends in the inbox step with the
    /// email and the time of the link, and the page shows no receipt and no way to manage a booking that does not exist.
    /// </summary>
    [Fact]
    public void Book_VerificationOn_EndsInTheInboxWithoutAReceipt()
    {
        // Arrange
        IRenderedComponent<Book> page = AtTheDetails(verificationRequired: true);

        // Act
        page.Button("Send booking request").Click();

        // Assert
        Assert.Contains("We've sent an email with a link to rita@example.com.", page.Markup, StringComparison.Ordinal);
        Assert.Contains("12:30 UTC on 10 March 2026", page.Markup, StringComparison.Ordinal);
        Assert.True(page.HasButton("Send again"));
        Assert.DoesNotContain("Your confirmation", page.Markup, StringComparison.Ordinal);
        Assert.False(page.HasButton("Manage your booking"));
        Assert.Empty(page.FindAll($"input[name='{GuestSignInRegistration.TicketField}']"));
    }

    /// <summary>Positive: the step indicator names the last step after the mode, and marks it when the flow is there.</summary>
    [Fact]
    public void Book_VerificationOn_StepIndicatorEndsInTheInbox()
    {
        // Arrange
        IRenderedComponent<Book> page = AtTheDetails(verificationRequired: true);

        // Act
        page.Button("Send booking request").Click();

        // Assert
        Assert.Equal("Check your inbox", page.Find("li[aria-current='step']").TextContent.Trim());
    }

    /// <summary>State transition (ADR-0052 decision 6): "Send again" asks again and says that the earlier link stopped.</summary>
    [Fact]
    public void Book_SendAgain_AsksAgainAndShowsTheNotice()
    {
        // Arrange
        IRenderedComponent<Book> page = AtTheDetails(verificationRequired: true);
        page.Button("Send booking request").Click();

        // Act
        page.Button("Send again").Click();

        // Assert
        Assert.Contains("The link in the earlier email no longer works.", page.Markup, StringComparison.Ordinal);
        _requests.Received(2).AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>());
    }

    /// <summary>State transition: "Change your details" goes back to the form with the email that was typed.</summary>
    [Fact]
    public void Book_ChangeYourDetails_ShowsTheFormWithTheEmail()
    {
        // Arrange
        IRenderedComponent<Book> page = AtTheDetails(verificationRequired: true);
        page.Button("Send booking request").Click();

        // Act
        page.Button("Change your details").Click();

        // Assert
        Assert.Equal("rita@example.com", page.Find("#email").GetAttribute("value"));
    }

    #endregion

    #region The email hint

    /// <summary>
    /// Equivalence: the hint at the email field says what the email is for: the link with the verification on, the
    /// reference that finds the booking with it off. Neither promises a confirmation by mail.
    /// </summary>
    /// <param name="verificationRequired">The mode the API answers.</param>
    /// <param name="hint">The hint at the email field.</param>
    [Theory]
    [InlineData(true, "We email you a link here. Your room is booked only when you use it.")]
    [InlineData(false, "With your booking reference, it finds your booking again.")]
    public void Details_Mode_EmailHintSaysWhatTheEmailIsFor(bool verificationRequired, string hint)
    {
        // Act
        IRenderedComponent<Book> page = AtTheDetails(verificationRequired);

        // Assert
        Assert.Contains(hint, page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("We send the confirmation here.", page.Markup, StringComparison.Ordinal);
    }

    /// <summary>Equivalence: with the verification off the page books at once and ends on the receipt, as before MIL-088.</summary>
    [Fact]
    public void Book_VerificationOff_EndsOnTheReceipt()
    {
        // Arrange
        _access.BookAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>())
            .Returns(Ok(new PublicBookingDto(Receipt(9), new GuestSessionDto("guest-token-9", LinkLapses))));
        IRenderedComponent<Book> page = AtTheDetails(verificationRequired: false);

        // Act
        page.Button("Send booking request").Click();

        // Assert
        Assert.Contains("Your confirmation", page.Markup, StringComparison.Ordinal);
        _requests.DidNotReceiveWithAnyArgs().AskAsync(Arg.Any<CreatePublicBookingRequest>(), Arg.Any<CancellationToken>());
    }

    #endregion
}
