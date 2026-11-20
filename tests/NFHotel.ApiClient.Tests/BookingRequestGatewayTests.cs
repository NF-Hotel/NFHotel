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
/// How the client carries the booking request of the public site and its verification by email (MIL-088, ADR-0052): the
/// mode, the request that the API answers with 202, the token of the link in the body and never in the address, and the
/// codes of a refusal, those of the mail on a 503 among them (ADR-0053).
/// </summary>
public sealed class BookingRequestGatewayTests
{
    private const string Reference = "3f2504e0-4f89-41d3-9a0c-0305e82c3301";

    /// <summary>A token of the form of a link: 43 characters of base64url (ADR-0052 decision 2).</summary>
    private const string Token = "q8Zr3m0X7c1V2b4N6m8Q0w2E4r6T8y0U2i4O6p8A0s2";

    private static readonly Uri Base = new("http://api.test/");

    private static readonly DateTimeOffset ExpiresAt = new(2030, 1, 1, 12, 30, 0, TimeSpan.Zero);

    #region The mode

    /// <summary>Positive (ADR-0052 decision 8): the mode is a GET of the mode route, and its answer says whether the guest verifies.</summary>
    /// <param name="required">Whether the verification is on.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetModeAsync_Answer_GetsTheModeRouteAndKeepsTheMode(bool required)
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Envelope(new PublicBookingModeDto(required)));
        ApiBookingRequestService service = new(Gateway(handler));

        // Act
        Result<PublicBookingModeDto> result = await service.GetModeAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Get, "/api/bookings/public/mode"), (handler.Method, handler.PathAndQuery));
        Assert.Equal(required, result.Value.VerificationRequired);
    }

    #endregion

    #region Ask

    /// <summary>
    /// Positive (ADR-0052 decision 1): the request posts the stay and the guest to the request route, and the 202 with the
    /// time the link lapses is a success.
    /// </summary>
    [Fact]
    public async Task AskAsync_Accepted_PostsToTheRequestRouteAndKeepsTheTimeOfTheLink()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.Accepted, Envelope(new BookingRequestReceiptDto(ExpiresAt)));
        ApiBookingRequestService service = new(Gateway(handler));

        // Act
        Result<BookingRequestReceiptDto> result = await service.AskAsync(PublicRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Post, "/api/bookings/public/requests"), (handler.Method, handler.PathAndQuery));
        Assert.Equal(ExpiresAt, result.Value.ExpiresAt);
    }

    /// <summary>Positive: the body of the request is the stay and the new guest, as the public booking sends them.</summary>
    [Fact]
    public async Task AskAsync_Body_HoldsTheStayAndTheGuest()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.Accepted, Envelope(new BookingRequestReceiptDto(ExpiresAt)));
        ApiBookingRequestService service = new(Gateway(handler));

        // Act
        await service.AskAsync(PublicRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Contains("\"checkInDate\":\"2030-01-10\"", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"checkOutDate\":\"2030-01-12\"", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"guests\":2", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"email\":\"anna@example.com\"", handler.Body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Negative (ADR-0052 decision 6, ADR-0051): a refusal of the request keeps the code of the API, so the page can say
    /// which limit or rule it was.
    /// </summary>
    /// <param name="status">The status of the answer.</param>
    /// <param name="code">The code in the answer.</param>
    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, "booking.too_many_mails")]
    [InlineData(HttpStatusCode.TooManyRequests, "booking.too_many_requests")]
    [InlineData(HttpStatusCode.Conflict, "booking.room_type_not_available")]
    [InlineData(HttpStatusCode.NotFound, "booking.not_found")]
    [InlineData(HttpStatusCode.UnprocessableEntity, "guest.email_invalid")]
    public async Task AskAsync_Refused_KeepsTheCode(HttpStatusCode status, string code)
    {
        // Arrange
        ApiBookingRequestService service = new(Gateway(StubHandler.Json(status, $$"""{"value":null,"errors":["{{code}}"]}""")));

        // Act
        Result<BookingRequestReceiptDto> result = await service.AskAsync(PublicRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(code, result.FirstError?.Code);
    }

    /// <summary>
    /// Error handling (ADR-0053): the 503 of a mail that the relay did not take, or that the fuse held back, keeps its
    /// code, so the guest is told what happened and not that the hotel system is down.
    /// </summary>
    /// <param name="code">The code of the mail in the answer.</param>
    [Theory]
    [InlineData(ErrorCodes.Booking.MailNotSent)]
    [InlineData(ErrorCodes.Booking.MailPaused)]
    public async Task AskAsync_ServiceUnavailableWithAMailCode_KeepsTheCode(string code)
    {
        // Arrange
        ApiBookingRequestService service = new(Gateway(StubHandler.Json(HttpStatusCode.ServiceUnavailable, $$"""{"value":null,"errors":["{{code}}"]}""")));

        // Act
        Result<BookingRequestReceiptDto> result = await service.AskAsync(PublicRequest(), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(code, result.FirstError?.Code);
    }

    #endregion

    #region Verify

    /// <summary>
    /// Security (ADR-0052 decision 3, ASVS 14.2.1): the token goes in the body of a POST to the verification route, and the
    /// address holds no part of it.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_Token_IsInTheBodyAndNotInTheAddress()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, Envelope(Booked()));
        ApiBookingRequestService service = new(Gateway(handler));

        // Act
        await service.VerifyAsync(new VerifyBookingRequest(Token), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpMethod.Post, "/api/bookings/public/requests/verify"), (handler.Method, handler.PathAndQuery));
        Assert.Equal($$"""{"token":"{{Token}}"}""", handler.Body);
        Assert.DoesNotContain(Token, handler.PathAndQuery, StringComparison.Ordinal);
    }

    /// <summary>Positive (ADR-0052 decision 5): a verification that booked keeps the receipt and the guest session.</summary>
    [Fact]
    public async Task VerifyAsync_Booked_KeepsTheReceiptAndTheSession()
    {
        // Arrange
        ApiBookingRequestService service = new(Gateway(StubHandler.Json(HttpStatusCode.OK, Envelope(Booked()))));

        // Act
        Result<PublicBookingDto> result = await service.VerifyAsync(new VerifyBookingRequest(Token), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(Booked(), result.Value);
    }

    /// <summary>
    /// Negative (ADR-0052 decisions 3 and 5): a link that does not work, a room that went and the setting that is off keep
    /// the code of the API.
    /// </summary>
    /// <param name="status">The status of the answer.</param>
    /// <param name="code">The code in the answer.</param>
    [Theory]
    [InlineData(HttpStatusCode.UnprocessableEntity, "booking.verification_link_invalid")]
    [InlineData(HttpStatusCode.Conflict, "booking.room_type_not_available")]
    [InlineData(HttpStatusCode.NotFound, "booking.not_found")]
    public async Task VerifyAsync_Refused_KeepsTheCode(HttpStatusCode status, string code)
    {
        // Arrange
        ApiBookingRequestService service = new(Gateway(StubHandler.Json(status, $$"""{"value":null,"errors":["{{code}}"]}""")));

        // Act
        Result<PublicBookingDto> result = await service.VerifyAsync(new VerifyBookingRequest(Token), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(code, result.FirstError?.Code);
    }

    /// <summary>Negative: a missing gateway or request is a programming error, and no call is made.</summary>
    [Fact]
    public async Task Service_MissingGatewayOrRequest_Throws()
    {
        // Arrange
        StubHandler handler = StubHandler.Json(HttpStatusCode.OK, "{}");
        ApiBookingRequestService service = new(Gateway(handler));

        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new ApiBookingRequestService(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.AskAsync(null!, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentNullException>(() => service.VerifyAsync(null!, TestContext.Current.CancellationToken));
        Assert.Equal(0, handler.Calls);
    }

    #endregion

    #region A 503 of the gateway

    /// <summary>
    /// Equivalence: a 503 with any other code of the API is still the API that is unavailable; only the two codes of the
    /// mail are kept.
    /// </summary>
    [Fact]
    public async Task GetAsync_ServiceUnavailableWithAnotherCode_ReturnsApiUnavailable()
    {
        // Arrange
        ApiGateway gateway = Gateway(StubHandler.Json(HttpStatusCode.ServiceUnavailable, """{"value":null,"errors":["market_analysis_runner.unavailable"]}"""));

        // Act
        Result<Sample> result = await gateway.GetAsync<Sample>("api/x", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Unavailable, result.FirstError?.Code);
    }

    /// <summary>Boundary: a mail code under another server error than 503 is the API that is unavailable.</summary>
    /// <param name="status">A server error other than 503.</param>
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.GatewayTimeout)]
    public async Task PostAsync_OtherServerErrorWithAMailCode_ReturnsApiUnavailable(HttpStatusCode status)
    {
        // Arrange
        ApiGateway gateway = Gateway(StubHandler.Json(status, """{"value":null,"errors":["booking.mail_not_sent"]}"""));

        // Act
        Result<Sample> result = await gateway.PostAsync<Sample>("api/x", new Sample(1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Unavailable, result.FirstError?.Code);
    }

    /// <summary>Edge: a 503 page of a proxy in front of the API, which is not JSON, is the API that is unavailable.</summary>
    [Fact]
    public async Task PostAsync_ServiceUnavailablePageOfAProxy_ReturnsApiUnavailable()
    {
        // Arrange
        ApiGateway gateway = Gateway(StubHandler.Text(HttpStatusCode.ServiceUnavailable, "<html>booking.mail_not_sent</html>"));

        // Act
        Result<Sample> result = await gateway.PostAsync<Sample>("api/x", new Sample(1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.Unavailable, result.FirstError?.Code);
    }

    #endregion

    #region Helpers

    private static ApiGateway Gateway(StubHandler handler) =>
        new(new HttpClient(handler, disposeHandler: false) { BaseAddress = Base });

    private static string Envelope<TValue>(TValue value) =>
        JsonSerializer.Serialize(new ApiResponse<TValue>(value, []), ApiJson.Options);

    private static CreatePublicBookingRequest PublicRequest() =>
        new(
            new DateOnly(2030, 1, 10),
            new DateOnly(2030, 1, 12),
            RoomSize.Double,
            2,
            new GuestFields("Anna", "Jensen", "anna@example.com", "+4512345678", "Denmark", null));

    private static PublicBookingDto Booked() =>
        new(
            new BookingReceiptDto(Guid.Parse(Reference), new DateOnly(2030, 1, 10), new DateOnly(2030, 1, 12), 2, RoomSize.Double, BookingStatus.Pending),
            new GuestSessionDto("t", new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero)));

    private sealed record Sample(int N);

    #endregion
}
