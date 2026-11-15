using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Web.Features.Booking;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>
/// The presentation logic of the guest sign-in on <c>/booking</c> (MIL-018, UC-010): the session on success, and one text
/// for every refusal, so the page tells a guesser nothing.
/// </summary>
public sealed class GuestSignInViewModelTests
{
    private static readonly GuestSessionDto Session = new("the-guest-token", new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero), 123);

    private readonly IGuestAccessService _access = Substitute.For<IGuestAccessService>();

    #region Sign-in

    /// <summary>Positive: the number with its prefix and the email give the session, and no errors.</summary>
    [Fact]
    public async Task SignInAsync_RightNumberAndEmail_ReturnsTheSessionWithoutErrors()
    {
        // Arrange
        _access.SignInAsync(Arg.Any<BookingReceiptRequest>(), Arg.Any<CancellationToken>()).Returns(AdminSamples.Ok(Session));
        GuestSignInViewModel viewModel = Create();

        // Act
        GuestSessionDto? session = await viewModel.SignInAsync("FLZ-000123", "anna@example.com", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(Session, session);
        Assert.Empty(viewModel.Errors);
        await _access.Received(1).SignInAsync(new BookingReceiptRequest(123, "anna@example.com"), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Security: an unreadable number gets the text of a wrong email without a call, so it costs no attempt and tells
    /// nothing.
    /// </summary>
    /// <param name="number">A number that is not a booking number.</param>
    [Theory]
    [InlineData("FLZ-")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public async Task SignInAsync_UnreadableNumber_ShowsTheOneRefusalWithoutACall(string? number)
    {
        // Arrange
        GuestSignInViewModel viewModel = Create();

        // Act
        GuestSessionDto? session = await viewModel.SignInAsync(number, "anna@example.com", TestContext.Current.CancellationToken);

        // Assert: the null-forgiving operator fills an argument that DidNotReceiveWithAnyArgs ignores.
        Assert.Null(session);
        Assert.Equal([AdminSamples.Text(ErrorCodes.Api.GuestUnauthorized)], viewModel.Errors);
        await _access.DidNotReceiveWithAnyArgs().SignInAsync(default!, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Negative: the refusal of the API and the throttle keep their translated texts; the refusal is the same text as an
    /// unreadable number.
    /// </summary>
    /// <param name="code">The code of the API.</param>
    [Theory]
    [InlineData(ErrorCodes.Api.GuestUnauthorized)]
    [InlineData(ErrorCodes.Booking.TooManyLookups)]
    [InlineData(ErrorCodes.Booking.ReceiptEmailRequired)]
    public async Task SignInAsync_Refused_ReturnsNullWithTheTranslatedText(string code)
    {
        // Arrange
        _access.SignInAsync(Arg.Any<BookingReceiptRequest>(), Arg.Any<CancellationToken>()).Returns(AdminSamples.Fail<GuestSessionDto>(code));
        GuestSignInViewModel viewModel = Create();

        // Act
        GuestSessionDto? session = await viewModel.SignInAsync("FLZ-000123", "anna@example.com", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(session);
        Assert.Equal([AdminSamples.Text(code)], viewModel.Errors);
    }

    /// <summary>State transition: a success after a refusal clears the errors.</summary>
    [Fact]
    public async Task SignInAsync_SuccessAfterRefusal_ClearsTheErrors()
    {
        // Arrange
        _access.SignInAsync(Arg.Any<BookingReceiptRequest>(), Arg.Any<CancellationToken>())
            .Returns(AdminSamples.Fail<GuestSessionDto>(ErrorCodes.Api.GuestUnauthorized), AdminSamples.Ok(Session));
        GuestSignInViewModel viewModel = Create();
        await viewModel.SignInAsync("FLZ-000123", "bo@example.com", TestContext.Current.CancellationToken);

        // Act
        GuestSessionDto? session = await viewModel.SignInAsync("FLZ-000123", "anna@example.com", TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(session);
        Assert.Empty(viewModel.Errors);
    }

    /// <summary>Negative: the runner is required.</summary>
    [Fact]
    public void Constructor_NullRunner_Throws()
    {
        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new GuestSignInViewModel(null!));
    }

    #endregion

    #region Helpers

    private GuestSignInViewModel Create() => new(new PassThroughRunner<IGuestAccessService>(_access));

    #endregion
}
