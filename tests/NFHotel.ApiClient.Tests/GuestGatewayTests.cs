using System.Net;
using System.Text.Json;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.ApiClient.Tests;

/// <summary>
/// How the client carries the guest token and keeps the codes of the guest sign-in (MIL-018, ADR-0047, and since MIL-085
/// ADR-0049): one token per call, the staff token before the guest token, none on a sign-in, and the guest routes that
/// name no booking.
/// </summary>
public sealed class GuestGatewayTests
{
    private const string Reference = "3f2504e0-4f89-41d3-9a0c-0305e82c3301";

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

    /// <summary>
    /// Positive (ADR-0049 decision 2): the sign-in posts the reference as typed and the email to the guest route, with no
    /// caller token, and returns the session, which names no booking.
    /// </summary>
    [Fact]
    public async Task SignInAsync_Success_PostsToTheGuestRouteAndReturnsTheSession()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(
            HttpStatusCode.OK,
            """{"value":{"token":"t","expiresAt":"2030-01-01T10:00:00+00:00"},"errors":[]}""");
        ApiGuestAccessService service = new(Gateway(handler, new StaffCallContext { Token = "staff" }, new GuestCallContext { Token = "guest" }));

        // Act
        Result<GuestSessionDto> result = await service.SignInAsync(new GuestSignInRequest(Reference, "anna@example.com"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Post, "/api/auth/guest-login"), (handler.Method, handler.PathAndQuery));
        Assert.Contains($"\"reference\":\"{Reference}\"", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"email\":\"anna@example.com\"", handler.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("bookingId", handler.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Null(handler.StaffToken);
        Assert.Null(handler.GuestToken);
        Assert.Equal(new GuestSessionDto("t", new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero)), result.Value);
    }

    /// <summary>Negative: the one refusal and the limit keep their own codes.</summary>
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
        Result<GuestSessionDto> result = await service.SignInAsync(new GuestSignInRequest(Reference, "anna@example.com"), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(code, result.FirstError?.Code);
    }

    /// <summary>Positive (ADR-0049 decision 3): the public booking posts to the public route and keeps the receipt and the session.</summary>
    [Fact]
    public async Task BookAsync_Success_PostsToThePublicRouteAndKeepsTheReceiptAndTheSession()
    {
        // Arrange
        PublicBookingDto answer = new(Receipt(), new GuestSessionDto("t", new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero)));
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Envelope(answer));
        ApiGuestAccessService service = new(Gateway(handler));

        // Act
        Result<PublicBookingDto> result = await service.BookAsync(PublicRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Post, "/api/bookings/public"), (handler.Method, handler.PathAndQuery));
        Assert.Equal(answer, result.Value);
    }

    /// <summary>Negative: a refused public booking keeps the code of the API.</summary>
    [Fact]
    public async Task BookAsync_Refused_KeepsTheCode()
    {
        // Arrange
        ApiGateway gateway = Gateway(StubHandler.Json(HttpStatusCode.Conflict, """{"value":null,"errors":["booking.room_type_not_available"]}"""));

        // Act
        Result<PublicBookingDto> result = await new ApiGuestAccessService(gateway).BookAsync(PublicRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("booking.room_type_not_available", result.FirstError?.Code);
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

    #region The own booking of the guest

    /// <summary>
    /// Positive (ADR-0049 decision 3): the guest reads the own booking on the guest route, which names no booking, with
    /// the guest token and without a staff token, and gets the receipt.
    /// </summary>
    [Fact]
    public async Task GetAsync_GuestScope_ReadsTheGuestRouteWithTheGuestToken()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Envelope(Receipt()));
        ApiGuestBookingService service = new(Gateway(handler, new StaffCallContext(), new GuestCallContext { Token = "guest" }));

        // Act
        Result<BookingReceiptDto> result = await service.GetAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Get, "/api/guest/booking"), (handler.Method, handler.PathAndQuery));
        Assert.Equal("guest", handler.GuestToken);
        Assert.Null(handler.StaffToken);
        Assert.Equal(Receipt(), result.Value);
    }

    /// <summary>Positive: the cancellation posts to the guest route with no body that could name a booking.</summary>
    [Fact]
    public async Task CancelAsync_GuestScope_PostsToTheGuestCancelRoute()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Envelope(Receipt() with { Status = BookingStatus.Cancelled }));
        ApiGuestBookingService service = new(Gateway(handler, new StaffCallContext(), new GuestCallContext { Token = "guest" }));

        // Act
        Result<BookingReceiptDto> result = await service.CancelAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Post, "/api/guest/booking/cancel"), (handler.Method, handler.PathAndQuery));
        Assert.Equal("guest", handler.GuestToken);
        Assert.Equal(BookingStatus.Cancelled, result.Value.Status);
    }

    /// <summary>Positive: the move posts the two dates to the guest route, and no key of a booking or a room.</summary>
    [Fact]
    public async Task RescheduleAsync_GuestScope_PostsTheDatesOnly()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Envelope(Receipt()));
        ApiGuestBookingService service = new(Gateway(handler, new StaffCallContext(), new GuestCallContext { Token = "guest" }));

        // Act
        await service.RescheduleAsync(new GuestRescheduleRequest(new DateOnly(2030, 2, 1), new DateOnly(2030, 2, 3)), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Post, "/api/guest/booking/reschedule"), (handler.Method, handler.PathAndQuery));
        Assert.Contains("\"checkInDate\":\"2030-02-01\"", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"checkOutDate\":\"2030-02-03\"", handler.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("bookingId", handler.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("roomId", handler.Body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Negative: the one refusal of a guest keeps its code on every guest operation.</summary>
    [Fact]
    public async Task GuestOperations_Refused_KeepTheGuestCode()
    {
        // Arrange
        ApiGuestBookingService service = new(Gateway(StubHandler.Json(HttpStatusCode.Unauthorized, """{"errors":["api.guest_unauthorized"]}""")));
        CancellationToken token = TestContext.Current.CancellationToken;

        // Act
        Result<BookingReceiptDto>[] results =
        [
            await service.GetAsync(token),
            await service.CancelAsync(token),
            await service.RescheduleAsync(new GuestRescheduleRequest(new DateOnly(2030, 2, 1), new DateOnly(2030, 2, 3)), token),
        ];

        // Assert
        Assert.All(results, result => Assert.Equal(ErrorCodes.Api.GuestUnauthorized, result.FirstError?.Code));
    }

    /// <summary>Negative: a missing gateway or request is a programming error, and no call is made.</summary>
    [Fact]
    public async Task GuestBookingService_MissingGatewayOrRequest_Throws()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, "{}");
        ApiGuestBookingService service = new(Gateway(handler));

        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new ApiGuestBookingService(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.RescheduleAsync(null!, TestContext.Current.CancellationToken));
        Assert.Null(handler.Method);
    }

    #endregion

    #region Helpers

    private static ApiGateway Gateway(StubHandler handler, StaffCallContext? staff = null, GuestCallContext? guest = null) =>
        new(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base }, staff, guest);

    private static string Envelope<TValue>(TValue value) =>
        JsonSerializer.Serialize(new ApiResponse<TValue>(value, []), ApiJson.Options);

    private static CreatePublicBookingRequest PublicRequest() =>
        new(
            new DateOnly(2030, 1, 10),
            new DateOnly(2030, 1, 12),
            RoomSize.Double,
            2,
            new GuestFields("Anna", "Jensen", "anna@example.com", "+4512345678", "Denmark", null));

    private static BookingReceiptDto Receipt() =>
        new(Guid.Parse(Reference), new DateOnly(2030, 1, 10), new DateOnly(2030, 1, 12), 2, RoomSize.Double, BookingStatus.Pending);

    private sealed record Sample(int N);

    #endregion
}
