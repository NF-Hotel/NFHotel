using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Web.Features.Booking;

using NSubstitute;

namespace NFHotel.Web.Tests;

/// <summary>
/// The presentation logic of the guest sign-in on <c>/booking</c> (MIL-018, UC-010, and since MIL-085 ADR-0049): the
/// session on success, and one text for every refusal, so the page tells a guesser nothing. The page reads no reference
/// itself: the API is the one place that decides what a reference is, and it counts every try.
/// </summary>
public sealed class GuestSignInViewModelTests
{
    private const string Reference = "3f2504e0-4f89-41d3-9a0c-0305e82c3301";

    private static readonly GuestSessionDto Session = new("the-guest-token", new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private readonly IGuestAccessService _access = Substitute.For<IGuestAccessService>();

    #region Sign-in

    /// <summary>Positive: the reference and the email, as the guest typed them, give the session and no errors.</summary>
    [Fact]
    public async Task SignInAsync_ReferenceAndEmail_ReturnsTheSessionWithoutErrors()
    {
        // Arrange
        _access.SignInAsync(Arg.Any<GuestSignInRequest>(), Arg.Any<CancellationToken>()).Returns(AdminSamples.Ok(Session));
        GuestSignInViewModel viewModel = Create();

        // Act
        GuestSessionDto? session = await viewModel.SignInAsync($" {Reference.ToUpperInvariant()} ", "Anna@Example.com", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(Session, session);
        Assert.Empty(viewModel.Errors);
        await _access.Received(1).SignInAsync(
            new GuestSignInRequest($" {Reference.ToUpperInvariant()} ", "Anna@Example.com"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Security (ADR-0049 decisions 5 and 6): a text that is no reference, an old booking number among them, goes to the
    /// API as typed, so the try is counted against the address of the caller, and it gets the one refusal.
    /// </summary>
    /// <param name="text">A text that is not a reference.</param>
    [Theory]
    [InlineData("FLZ-000123")]
    [InlineData("123")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public async Task SignInAsync_TextThatIsNoReference_GoesToTheApiAndGetsTheOneRefusal(string? text)
    {
        // Arrange
        _access.SignInAsync(Arg.Any<GuestSignInRequest>(), Arg.Any<CancellationToken>())
            .Returns(AdminSamples.Fail<GuestSessionDto>(ErrorCodes.Api.GuestUnauthorized));
        GuestSignInViewModel viewModel = Create();

        // Act
        GuestSessionDto? session = await viewModel.SignInAsync(text, "anna@example.com", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(session);
        Assert.Equal([AdminSamples.Text(ErrorCodes.Api.GuestUnauthorized)], viewModel.Errors);
        await _access.Received(1).SignInAsync(new GuestSignInRequest(text, "anna@example.com"), Arg.Any<CancellationToken>());
    }

    /// <summary>Negative: the refusal of the API and the limits keep their translated texts.</summary>
    /// <param name="code">The code of the API.</param>
    [Theory]
    [InlineData(ErrorCodes.Api.GuestUnauthorized)]
    [InlineData(ErrorCodes.Booking.TooManyLookups)]
    [InlineData(ErrorCodes.Booking.ReceiptEmailRequired)]
    public async Task SignInAsync_Refused_ReturnsNullWithTheTranslatedText(string code)
    {
        // Arrange
        _access.SignInAsync(Arg.Any<GuestSignInRequest>(), Arg.Any<CancellationToken>()).Returns(AdminSamples.Fail<GuestSessionDto>(code));
        GuestSignInViewModel viewModel = Create();

        // Act
        GuestSessionDto? session = await viewModel.SignInAsync(Reference, "anna@example.com", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(session);
        Assert.Equal([AdminSamples.Text(code)], viewModel.Errors);
    }

    /// <summary>
    /// Security (ADR-0049 decision 6): the texts of the refusal and of the limit name no booking number and no reference,
    /// so the page tells a guesser nothing about the format either.
    /// </summary>
    /// <param name="code">The code of the API.</param>
    [Theory]
    [InlineData(ErrorCodes.Api.GuestUnauthorized)]
    [InlineData(ErrorCodes.Booking.TooManyLookups)]
    public void Texts_RefusalAndLimit_NameNoBookingNumber(string code)
    {
        // Act
        string text = AdminSamples.Text(code);

        // Assert
        Assert.DoesNotContain("booking number", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FLZ", text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>State transition: a success after a refusal clears the errors.</summary>
    [Fact]
    public async Task SignInAsync_SuccessAfterRefusal_ClearsTheErrors()
    {
        // Arrange
        _access.SignInAsync(Arg.Any<GuestSignInRequest>(), Arg.Any<CancellationToken>())
            .Returns(AdminSamples.Fail<GuestSessionDto>(ErrorCodes.Api.GuestUnauthorized), AdminSamples.Ok(Session));
        GuestSignInViewModel viewModel = Create();
        await viewModel.SignInAsync(Reference, "bo@example.com", TestContext.Current.CancellationToken);

        // Act
        GuestSessionDto? session = await viewModel.SignInAsync(Reference, "anna@example.com", TestContext.Current.CancellationToken);

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
