using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Web.Features.Booking;

using NSubstitute;

using static NFHotel.Web.Tests.AdminSamples;

namespace NFHotel.Web.Tests;

/// <summary>
/// The presentation logic of the page of the link in the mail of a booking request (MIL-088, ADR-0052 decisions 3 and 5):
/// the token goes to the API in the body, a booked room answers with the guest session, and a link that does not work
/// gets one text.
/// </summary>
public sealed class BookingVerifyViewModelTests
{
    #region Setup

    private const string Token = "q8Zr3m0X7c1V2b4N6m8Q0w2E4r6T8y0U2i4O6p8A0s2";

    /// <summary>The one text of a link that does not work, written out so the test pins the words.</summary>
    private const string LinkInvalidText =
        "This link doesn't work any more. Ask for the booking again, and we'll email you a new link.";

    private static readonly GuestSessionDto Session = new("guest-token-9", new DateTimeOffset(2026, 3, 10, 14, 0, 0, TimeSpan.Zero));

    private readonly IBookingRequestService _requests = Substitute.For<IBookingRequestService>();

    private static CancellationToken Call => TestContext.Current.CancellationToken;

    private BookingVerifyViewModel Create() => new(new PassThroughRunner<IBookingRequestService>(_requests));

    private void VerifyAnswers(Task<Result<PublicBookingDto>> answer) =>
        _requests.VerifyAsync(Arg.Any<VerifyBookingRequest>(), Arg.Any<CancellationToken>()).Returns(answer);

    #endregion

    #region Verify

    /// <summary>Negative: the runner of the requests is required.</summary>
    [Fact]
    public void Constructor_MissingRunner_Throws()
    {
        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new BookingVerifyViewModel(null!));
    }

    /// <summary>Positive (ADR-0052 decision 5): the token goes to the API as it came, and the room booked gives the session.</summary>
    [Fact]
    public async Task VerifyAsync_Booked_SendsTheTokenAndReturnsTheSession()
    {
        // Arrange
        VerifyAnswers(Ok(new PublicBookingDto(Receipt(9), Session)));
        BookingVerifyViewModel viewModel = Create();

        // Act
        GuestSessionDto? session = await viewModel.VerifyAsync(Token, Call);

        // Assert
        Assert.Equal(Session, session);
        Assert.False(viewModel.HasFailed);
        await _requests.Received(1).VerifyAsync(new VerifyBookingRequest(Token), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Equivalence (ADR-0052 decision 3): a link that is unknown, used or lapsed, and a link of a verification that was
    /// switched off since, get the one text of a link that does not work, and no session.
    /// </summary>
    /// <param name="code">The code of the refusal.</param>
    [Theory]
    [InlineData(ErrorCodes.Booking.VerificationLinkInvalid)]
    [InlineData(ErrorCodes.Booking.NotFound)]
    public async Task VerifyAsync_LinkDoesNotWork_ShowsTheOneText(string code)
    {
        // Arrange
        VerifyAnswers(Fail<PublicBookingDto>(code));
        BookingVerifyViewModel viewModel = Create();

        // Act
        GuestSessionDto? session = await viewModel.VerifyAsync(Token, Call);

        // Assert
        Assert.Null(session);
        Assert.True(viewModel.HasFailed);
        Assert.Equal([LinkInvalidText], viewModel.Errors);
    }

    /// <summary>Error handling (ADR-0052 decision 5): the room that went between the mail and the button says so.</summary>
    [Fact]
    public async Task VerifyAsync_RoomGone_ShowsTheTextOfTheRoom()
    {
        // Arrange
        VerifyAnswers(Fail<PublicBookingDto>(ErrorCodes.Booking.RoomTypeNotAvailable));
        BookingVerifyViewModel viewModel = Create();

        // Act
        await viewModel.VerifyAsync(Token, Call);

        // Assert
        Assert.Equal([Text(ErrorCodes.Booking.RoomTypeNotAvailable)], viewModel.Errors);
    }

    /// <summary>Error handling: an API that cannot be reached shows its own text, not the text of a link that does not work.</summary>
    [Fact]
    public async Task VerifyAsync_ApiUnavailable_ShowsItsText()
    {
        // Arrange
        VerifyAnswers(Fail<PublicBookingDto>(ErrorCodes.Api.Unavailable));
        BookingVerifyViewModel viewModel = Create();

        // Act
        await viewModel.VerifyAsync(Token, Call);

        // Assert
        Assert.Equal([Text(ErrorCodes.Api.Unavailable)], viewModel.Errors);
    }

    /// <summary>
    /// Boundary (ADR-0052 decision 3): no token, an empty one and one longer than the API reads get the one text without a
    /// call; a token of the longest length the API reads is sent.
    /// </summary>
    /// <param name="length">The length of the token, or -1 for none.</param>
    /// <param name="isSent">Whether the token reaches the API.</param>
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(VerifyBookingRequest.MaxTokenLength, true)]
    [InlineData(VerifyBookingRequest.MaxTokenLength + 1, false)]
    public async Task VerifyAsync_LengthOfTheToken_DecidesTheCall(int length, bool isSent)
    {
        // Arrange
        VerifyAnswers(Fail<PublicBookingDto>(ErrorCodes.Booking.VerificationLinkInvalid));
        BookingVerifyViewModel viewModel = Create();
        string? token = length < 0 ? null : new string('a', length);

        // Act
        await viewModel.VerifyAsync(token, Call);

        // Assert
        Assert.Equal([LinkInvalidText], viewModel.Errors);
        Assert.Equal(isSent ? 1 : 0, _requests.ReceivedCalls().Count());
    }

    #endregion
}
