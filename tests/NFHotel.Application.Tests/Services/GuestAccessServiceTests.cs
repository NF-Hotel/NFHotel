using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;

namespace NFHotel.Application.Tests.Services;

/// <summary>
/// The guest sign-in of MIL-018 (ADR-0047, UC-010): the number and the email of a booking give a guest session for that
/// booking; every other case gives the one refusal, and the public booking signs its guest in.
/// </summary>
public sealed class GuestAccessServiceTests
{
    #region Sign-in

    /// <summary>Positive: the right number and email give a session for exactly that booking (ADR-0047 decision 1 and 2).</summary>
    [Fact]
    public async Task SignInAsync_RightNumberAndEmail_GivesASessionForTheBooking()
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 3);

        // Act
        Result<GuestSessionDto> result = await world.GuestAccessService()
            .SignInAsync(new BookingReceiptRequest(booking.Id, "anna@example.com"), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(booking.Id, result.Value.BookingId);
        Assert.Equal(world.GuestTokens.ExpiresAt, result.Value.ExpiresAt);
        Assert.Equal([booking.Id], world.GuestTokens.Issued);
    }

    /// <summary>Equivalence: the email is compared without case and without the spaces around it (ADR-0047 decision 3).</summary>
    /// <param name="email">The email as the guest types it.</param>
    [Theory]
    [InlineData("ANNA@EXAMPLE.COM")]
    [InlineData("  Anna@Example.com ")]
    public async Task SignInAsync_EmailInAnotherCaseOrWithSpaces_GivesASession(string email)
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);

        // Act
        Result<GuestSessionDto> result = await world.GuestAccessService()
            .SignInAsync(new BookingReceiptRequest(booking.Id, email), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// Security: a wrong email, an unknown number and a cancelled booking give the same one refusal, and no token is
    /// issued (ADR-0047 decisions 3 and 7, MIL-018 criterion 3).
    /// </summary>
    /// <param name="refusal">Which of the three is tried.</param>
    [Theory]
    [InlineData("wrong email")]
    [InlineData("unknown number")]
    [InlineData("cancelled booking")]
    public async Task SignInAsync_WrongEmailUnknownNumberOrCancelled_GivesTheOneRefusal(string refusal)
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 3);
        if (refusal == "cancelled booking")
        {
            booking.Cancel();
        }

        BookingReceiptRequest request = refusal switch
        {
            "wrong email" => new BookingReceiptRequest(booking.Id, "bo@example.com"),
            "unknown number" => new BookingReceiptRequest(booking.Id + 100, "anna@example.com"),
            _ => new BookingReceiptRequest(booking.Id, "anna@example.com"),
        };

        // Act
        Result<GuestSessionDto> result = await world.GuestAccessService().SignInAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([ErrorCodes.Api.GuestUnauthorized], result.Errors.Select(error => error.Code));
        Assert.Empty(world.GuestTokens.Issued);
    }

    /// <summary>
    /// Security: every failure counts against the number in the throttle of the lookup, and a blocked number is refused
    /// with its own code even with the right email (ADR-0047 decision 3: 429 after five failures in 15 minutes).
    /// </summary>
    [Fact]
    public async Task SignInAsync_FailureAndBlockedNumber_CountAndAreRefusedWithTooManyLookups()
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);
        ILoginThrottle throttle = ServiceWorld.OpenThrottle();
        GuestAccessService service = world.GuestAccessService(receiptThrottle: throttle);
        string key = $"booking-receipt:{booking.Id}";

        // Act
        Result<GuestSessionDto> failed = await service.SignInAsync(new BookingReceiptRequest(booking.Id, "bo@example.com"), TestContext.Current.CancellationToken);
        throttle.TryCountAttempt(key).Returns(false);
        Result<GuestSessionDto> blocked = await service.SignInAsync(new BookingReceiptRequest(booking.Id, "anna@example.com"), TestContext.Current.CancellationToken);

        // Assert: the null-forgiving operator fills an argument that DidNotReceiveWithAnyArgs ignores.
        Assert.Equal(ErrorCodes.Api.GuestUnauthorized, failed.FirstError?.Code);
        throttle.Received(2).TryCountAttempt(key);
        throttle.DidNotReceiveWithAnyArgs().Reset(default!);
        Assert.Equal(ErrorCodes.Booking.TooManyLookups, blocked.FirstError?.Code);
        Assert.Empty(world.GuestTokens.Issued);
    }

    /// <summary>
    /// Security: the right email of a cancelled booking is refused and keeps the count, as a wrong email does, so the
    /// throttle cannot tell a guesser that the email was right (ADR-0047 decisions 3 and 7).
    /// </summary>
    [Fact]
    public async Task SignInAsync_RightEmailOfACancelledBooking_IsRefusedAndKeepsTheCount()
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 3);
        booking.Cancel();
        ILoginThrottle throttle = ServiceWorld.OpenThrottle();

        // Act
        Result<GuestSessionDto> result = await world.GuestAccessService(receiptThrottle: throttle)
            .SignInAsync(new BookingReceiptRequest(booking.Id, "anna@example.com"), TestContext.Current.CancellationToken);

        // Assert: the null-forgiving operator fills an argument that DidNotReceiveWithAnyArgs ignores.
        Assert.Equal(ErrorCodes.Api.GuestUnauthorized, result.FirstError?.Code);
        throttle.Received(1).TryCountAttempt($"booking-receipt:{booking.Id}");
        throttle.DidNotReceiveWithAnyArgs().Reset(default!);
        Assert.Empty(world.GuestTokens.Issued);
    }

    /// <summary>Negative: a missing email keeps the code of the lookup, which the API answers with 422, and issues nothing.</summary>
    /// <param name="email">A missing or blank email.</param>
    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task SignInAsync_NoEmail_KeepsTheCodeOfTheLookup(string? email)
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);

        // Act
        Result<GuestSessionDto> result = await world.GuestAccessService()
            .SignInAsync(new BookingReceiptRequest(booking.Id, email), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.ReceiptEmailRequired, result.FirstError?.Code);
        Assert.Empty(world.GuestTokens.Issued);
    }

    /// <summary>Negative: a missing request is a programming error.</summary>
    [Fact]
    public async Task SignInAsync_NullRequest_Throws()
    {
        // Arrange
        ServiceWorld world = new();

        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        await Assert.ThrowsAsync<ArgumentNullException>(() => world.GuestAccessService().SignInAsync(null!, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Public booking

    /// <summary>Positive: the public booking answers with the booking and a session for it (ADR-0047 decision 2).</summary>
    [Fact]
    public async Task BookAsync_FreeRoomOfTheType_AnswersWithTheBookingAndItsSession()
    {
        // Arrange
        ServiceWorld world = new();
        world.AddRoom("201", 2, RoomSize.Double, 2);
        CreatePublicBookingRequest request = new(
            world.Clock.Today.AddDays(4),
            world.Clock.Today.AddDays(6),
            RoomSize.Double,
            2,
            new GuestFields("Maja", "Holm", "maja@example.com", "+4511223344", "Denmark", "P7654321"));

        // Act
        Result<PublicBookingDto> result = await world.GuestAccessService().BookAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(result.Value.Booking.BookingId, result.Value.Session.BookingId);
        Assert.Equal([result.Value.Booking.BookingId], world.GuestTokens.Issued);
        Assert.Equal(BookingStatus.Pending, result.Value.Booking.Status);
    }

    /// <summary>Negative: a refused booking keeps the codes of the booking and issues no session.</summary>
    [Fact]
    public async Task BookAsync_NoFreeRoomOfTheType_KeepsTheCodeAndIssuesNoSession()
    {
        // Arrange
        ServiceWorld world = new();
        CreatePublicBookingRequest request = new(
            world.Clock.Today.AddDays(4),
            world.Clock.Today.AddDays(6),
            RoomSize.Suite,
            1,
            ServiceWorld.ValidGuestFields());

        // Act
        Result<PublicBookingDto> result = await world.GuestAccessService().BookAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Booking.RoomTypeNotAvailable, result.FirstError?.Code);
        Assert.Empty(world.GuestTokens.Issued);
    }

    /// <summary>Negative: the dependencies and the request are required.</summary>
    [Fact]
    public async Task Constructor_MissingDependencyOrRequest_Throws()
    {
        // Arrange
        ServiceWorld world = new();

        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new GuestAccessService(null!, world.GuestTokens));
        Assert.Throws<ArgumentNullException>(() => new GuestAccessService(world.BookingService(), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => world.GuestAccessService().BookAsync(null!, TestContext.Current.CancellationToken));
    }

    #endregion
}
