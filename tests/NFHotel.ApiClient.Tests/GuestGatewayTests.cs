using System.Net;
using System.Text.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.ApiClient.Tests;

/// <summary>
/// How the client carries the guest token and keeps the codes of the guest sign-in (MIL-018, ADR-0047): one token per
/// call, the staff token before the guest token, and none on a sign-in.
/// </summary>
public sealed class GuestGatewayTests
{
    private static readonly Uri Base = new("http://api.test/");

    #region The guest header

    /// <summary>Positive: a call in a scope that holds only a guest token sends it in the guest header, and no staff header.</summary>
    [Fact]
    public async Task GetAsync_WithAGuestTokenOnly_SendsTheGuestHeader()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, """{"value":{"n":1},"errors":[]}""");
        ApiGateway gateway = Gateway(handler, new StaffCallContext(), new GuestCallContext { Token = "the-guest-token" });

        // Act
        await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("the-guest-token", handler.GuestToken);
        Assert.Null(handler.StaffToken);
    }

    /// <summary>
    /// Security: a scope that holds both tokens sends only the staff token, so a call never carries two identities and
    /// the API never has to choose.
    /// </summary>
    [Fact]
    public async Task GetAsync_WithAStaffAndAGuestToken_SendsOnlyTheStaffToken()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, """{"value":{"n":1},"errors":[]}""");
        ApiGateway gateway = Gateway(handler, new StaffCallContext { Token = "the-staff-token" }, new GuestCallContext { Token = "the-guest-token" });

        // Act
        await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("the-staff-token", handler.StaffToken);
        Assert.Null(handler.GuestToken);
    }

    /// <summary>Equivalence: a call without a guest token, such as the public booking pages make, sends no guest header.</summary>
    /// <param name="token">A missing or empty guest token.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetAsync_WithoutAGuestToken_SendsNoGuestHeader(string? token)
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, """{"value":{"n":1},"errors":[]}""");
        ApiGateway gateway = Gateway(handler, new StaffCallContext(), new GuestCallContext { Token = token });

        // Act
        await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(handler.GuestToken);
        Assert.Null(handler.StaffToken);
    }

    /// <summary>Negative: the sign-in never sends a guest token, even if the scope holds one, so an old session cannot ride on a new sign-in.</summary>
    [Fact]
    public async Task PostAnonymousAsync_ScopeWithAGuestToken_SendsNoGuestHeader()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, """{"value":{"n":1},"errors":[]}""");
        ApiGateway gateway = Gateway(handler, new StaffCallContext(), new GuestCallContext { Token = "old-guest-token" });

        // Act
        await gateway.PostAnonymousAsync<Sample>("api/auth/guest-login", new Sample(1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(handler.GuestToken);
    }

    /// <summary>
    /// Positive: the one refusal of a guest (401 with the guest code and no value in the body) keeps the guest code, so the
    /// page can say that the number and the email do not match.
    /// </summary>
    [Fact]
    public async Task GetAsync_UnauthorizedWithTheGuestCode_KeepsTheGuestCode()
    {
        // Arrange
        ApiGateway gateway = Gateway(StubHandler.Json(HttpStatusCode.Unauthorized, """{"errors":["api.guest_unauthorized"]}"""));

        // Act
        Result<Sample> result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.GuestUnauthorized, result.FirstError?.Code);
    }

    #endregion

    #region The guest sign-in and the public booking

    /// <summary>Positive: the sign-in posts the number and the email to the guest route, with no caller token, and returns the session.</summary>
    [Fact]
    public async Task SignInAsync_Success_PostsToTheGuestRouteAndReturnsTheSession()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(
            HttpStatusCode.OK,
            """{"value":{"token":"t","expiresAt":"2030-01-01T10:00:00+00:00","bookingId":7},"errors":[]}""");
        ApiGuestAccessService service = new(Gateway(handler, new StaffCallContext { Token = "staff" }, new GuestCallContext { Token = "guest" }));

        // Act
        Result<GuestSessionDto> result = await service.SignInAsync(new BookingReceiptRequest(7, "anna@example.com"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Post, "/api/auth/guest-login"), (handler.Method, handler.PathAndQuery));
        Assert.Contains("anna@example.com", handler.Body, StringComparison.Ordinal);
        Assert.Null(handler.StaffToken);
        Assert.Null(handler.GuestToken);
        Assert.Equal(new GuestSessionDto("t", new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero), 7), result.Value);
    }

    /// <summary>Negative: the one refusal and the throttle keep their own codes.</summary>
    /// <param name="status">The status of the answer.</param>
    /// <param name="code">The code in the answer.</param>
    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "api.guest_unauthorized")]
    [InlineData(HttpStatusCode.TooManyRequests, "booking.too_many_lookups")]
    public async Task SignInAsync_Refused_KeepsTheCode(HttpStatusCode status, string code)
    {
        // Arrange
        ApiGuestAccessService service = new(Gateway(StubHandler.Json(status, $$"""{"errors":["{{code}}"]}""")));

        // Act
        Result<GuestSessionDto> result = await service.SignInAsync(new BookingReceiptRequest(7, "anna@example.com"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(code, result.FirstError?.Code);
    }

    /// <summary>Positive: the public booking posts to the public route and keeps both the booking and the session.</summary>
    [Fact]
    public async Task BookAsync_Success_PostsToThePublicRouteAndKeepsTheBookingAndTheSession()
    {
        // Arrange
        PublicBookingDto answer = new(Booked(9), new GuestSessionDto("t", new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero), 9));
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Envelope(answer));
        ApiGuestAccessService service = new(Gateway(handler));

        // Act
        Result<PublicBookingDto> result = await service.BookAsync(PublicRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Post, "/api/bookings/public"), (handler.Method, handler.PathAndQuery));
        Assert.Equal(9, result.Value.Booking.BookingId);
        Assert.Equal("t", result.Value.Session.Token);
    }

    /// <summary>
    /// Equivalence: the booking service reads the same answer and keeps only the booking, as its use case promises, so
    /// the staff side of the flow is unchanged.
    /// </summary>
    [Fact]
    public async Task CreatePublicAsync_AnswerWithASession_ReturnsTheBooking()
    {
        // Arrange
        PublicBookingDto answer = new(Booked(9), new GuestSessionDto("t", new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero), 9));
        ApiBookingService service = new(Gateway(StubHandler.Json(HttpStatusCode.OK, Envelope(answer))));

        // Act
        Result<BookingDetailsDto> result = await service.CreatePublicAsync(PublicRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(9, result.Value.BookingId);
    }

    /// <summary>Negative: a refused public booking keeps the codes of the API in both services.</summary>
    [Fact]
    public async Task CreatePublicAsync_Refused_KeepsTheCode()
    {
        // Arrange
        ApiGateway gateway = Gateway(StubHandler.Json(HttpStatusCode.Conflict, """{"value":null,"errors":["booking.room_type_not_available"]}"""));

        // Act
        Result<BookingDetailsDto> booking = await new ApiBookingService(gateway).CreatePublicAsync(PublicRequest(), TestContext.Current.CancellationToken);
        Result<PublicBookingDto> session = await new ApiGuestAccessService(gateway).BookAsync(PublicRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("booking.room_type_not_available", booking.FirstError?.Code);
        Assert.Equal("booking.room_type_not_available", session.FirstError?.Code);
    }

    /// <summary>Negative: a missing gateway or request is a programming error.</summary>
    [Fact]
    public async Task Constructor_MissingGatewayOrRequest_Throws()
    {
        // Arrange
        ApiGuestAccessService service = new(Gateway(StubHandler.Json(HttpStatusCode.OK, "{}")));

        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new ApiGuestAccessService(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.SignInAsync(null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.BookAsync(null!, TestContext.Current.CancellationToken));
    }

    #endregion

    #region Helpers

    private static ApiGateway Gateway(StubHandler handler, StaffCallContext? staff = null, GuestCallContext? guest = null) =>
        new(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base }, staff, guest);

    private static string Envelope(PublicBookingDto value) =>
        JsonSerializer.Serialize(new ApiResponse<PublicBookingDto>(value, []), ApiJson.Options);

    private static CreatePublicBookingRequest PublicRequest() =>
        new(
            new DateOnly(2030, 1, 10),
            new DateOnly(2030, 1, 12),
            RoomSize.Double,
            2,
            new GuestFields("Anna", "Jensen", "anna@example.com", "+4512345678", "Denmark", null));

    private static BookingDetailsDto Booked(int id) =>
        new(
            id,
            $"FLZ-{id:D6}",
            new DateOnly(2030, 1, 10),
            new DateOnly(2030, 1, 12),
            new DateOnly(2030, 1, 12),
            2,
            BookingStatus.Pending,
            null,
            null,
            null,
            new RoomListItemDto(1, "101", 1, RoomSize.Double, 2, RoomStatus.Available, HousekeepingStatus.Clean),
            new GuestListItemDto(1, "Anna", "Jensen", "Anna Jensen", "anna@example.com", "+4512345678", "Denmark", false),
            new BookingActionsDto(true, false, false, true, true));

    private sealed record Sample(int N);

    #endregion
}
