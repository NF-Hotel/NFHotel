using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases.Staff;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;
using NFHotel.TestSupport;

namespace NFHotel.Api.Tests;

/// <summary>
/// The guest sign-in over HTTP (MIL-018, ADR-0047, UC-010), through the real pipeline of the API: the staff token, the
/// guest token, the permission attribute and the use cases. One region per Go/No-Go criterion of the milestone that an
/// API test proves (1 to 4).
/// </summary>
/// <remarks>
/// The addresses and bodies are written out instead of taken from the contracts, as in
/// <see cref="BookingReadProtectionTests"/>, so the tests pin what the web application depends on.
/// </remarks>
public sealed class GuestAccessTests : IClassFixture<ApiFactory>
{
    private const string GuestLoginPath = "api/auth/guest-login";
    private const string PublicBookingPath = "api/bookings/public";
    private const string ReschedulePath = "api/bookings/reschedule";
    private const string OtherSigningKey = "another-signing-key-0123456789-another-key";
    private const string PassportNumber = "P9876543";
    private const int MaxFailures = 5;

    private const string ReadBooking = "read the booking";
    private const string ReadGuest = "read the guest of the booking";
    private const string Cancel = "cancel the booking";
    private const string Reschedule = "move the booking to other dates";

    private readonly ApiFactory _factory;

    public GuestAccessTests(ApiFactory factory)
    {
        _factory = factory;
    }

    #region Criterion 1: no token

    /// <summary>
    /// Negative (criterion 1): without a staff or a guest token, the four guest operations get 401 with the code of a
    /// missing sign-in, and the booking stays as it was.
    /// </summary>
    /// <param name="operation">The operation.</param>
    [Theory]
    [InlineData(ReadBooking)]
    [InlineData(ReadGuest)]
    [InlineData(Cancel)]
    [InlineData(Reschedule)]
    public async Task GuestOperation_NoStaffOrGuestToken_ReturnsUnauthorized(string operation)
    {
        // Arrange
        (Booking booking, _) = SeedBooking();

        // Act
        (HttpStatusCode status, string text) = await RunAsync(operation, booking, guestToken: null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, status);
        Assert.Contains(ErrorCodes.Api.StaffUnauthorized, text, StringComparison.Ordinal);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    /// <summary>
    /// Positive (criterion 1 and ADR-0047 decision 2): the free rooms and a new booking need no token, and the booking's
    /// answer carries a guest session that opens exactly that booking.
    /// </summary>
    [Fact]
    public async Task PublicBooking_NoToken_ReturnsTheBookingWithASessionThatOpensIt()
    {
        // Arrange
        ServiceWorld world = _factory.World;
        world.AddRoom(NextRoomNumber(), 2, RoomSize.Suite, 4);
        DateOnly checkIn = world.Clock.Today.AddDays(40);
        CreatePublicBookingRequest request = new(
            checkIn,
            checkIn.AddDays(2),
            RoomSize.Suite,
            2,
            new GuestFields("Maja", "Holm", "maja.holm@example.com", "+4511223344", "Denmark", PassportNumber));
        using HttpClient client = _factory.CreateServiceClient();

        // Act
        using HttpResponseMessage rooms = await client.GetAsync(
            $"api/bookings/available-rooms?checkInDate={checkIn:yyyy-MM-dd}&checkOutDate={checkIn.AddDays(2):yyyy-MM-dd}",
            TestContext.Current.CancellationToken);
        using HttpResponseMessage created = await client.PostAsJsonAsync(PublicBookingPath, request, ApiJson.Options, TestContext.Current.CancellationToken);
        string createdText = await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        PublicBookingDto booked = ValueOf<PublicBookingDto>(createdText);
        (HttpStatusCode ownStatus, _) = await SendAsync(HttpMethod.Get, $"api/bookings/{booked.Booking.BookingId}", booked.Session.Token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, rooms.StatusCode);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        Assert.Equal(booked.Booking.BookingId, booked.Session.BookingId);
        Assert.False(string.IsNullOrWhiteSpace(booked.Session.Token));
        Assert.Equal(HttpStatusCode.OK, ownStatus);
        Assert.DoesNotContain(PassportNumber, createdText, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Criterion 2: the own booking only

    /// <summary>Positive (criterion 2): the guest token of a booking reads, cancels and moves that booking and reads its guest.</summary>
    /// <param name="operation">The operation.</param>
    [Theory]
    [InlineData(ReadBooking)]
    [InlineData(ReadGuest)]
    [InlineData(Cancel)]
    [InlineData(Reschedule)]
    public async Task GuestOperation_OwnBooking_IsAllowed(string operation)
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        string token = await SignInAsync(booking.Id, email);

        // Act
        (HttpStatusCode status, string text) = await RunAsync(operation, booking, token);

        // Assert
        Assert.True(status == HttpStatusCode.OK, $"{operation}: {(int)status} {text}");
    }

    /// <summary>
    /// Security (criterion 2): the guest token of booking A gets 403 on booking B, on the guest of B, and on the cancel and
    /// the move of B; B does not change.
    /// </summary>
    /// <param name="operation">The operation.</param>
    [Theory]
    [InlineData(ReadBooking)]
    [InlineData(ReadGuest)]
    [InlineData(Cancel)]
    [InlineData(Reschedule)]
    public async Task GuestOperation_AnotherBooking_ReturnsForbidden(string operation)
    {
        // Arrange
        (Booking own, string email) = SeedBooking();
        (Booking other, _) = SeedBooking();
        string token = await SignInAsync(own.Id, email);
        DateOnly startBefore = other.StartDate;

        // Act
        (HttpStatusCode status, string text) = await RunAsync(operation, other, token);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, status);
        Assert.Contains(ErrorCodes.Api.Forbidden, text, StringComparison.Ordinal);
        Assert.DoesNotContain(other.Id.ToString(CultureInfo.InvariantCulture) + ",", text, StringComparison.Ordinal);
        Assert.Equal(BookingStatus.Pending, other.Status);
        Assert.Equal(startBefore, other.StartDate);
    }

    /// <summary>Security: a guest moves the own booking to other dates, never to another room (UC-010).</summary>
    [Fact]
    public async Task Reschedule_OwnBookingToAnotherRoom_ReturnsForbidden()
    {
        // Arrange
        (Booking own, string email) = SeedBooking();
        (Booking other, _) = SeedBooking();
        string token = await SignInAsync(own.Id, email);
        int roomBefore = own.RoomId;

        // Act
        (HttpStatusCode status, _) = await SendAsync(
            HttpMethod.Post,
            ReschedulePath,
            token,
            new RescheduleBookingRequest(own.Id, own.StartDate.AddDays(20), own.EndDate.AddDays(20), other.RoomId));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, status);
        Assert.Equal(roomBefore, own.RoomId);
    }

    #endregion

    #region Criterion 3: one refusal, and the throttle

    /// <summary>Positive: the number and the email of the booking, in any letter case, give a session of two hours for it.</summary>
    [Fact]
    public async Task SignIn_RightNumberAndEmailInAnotherCase_ReturnsASessionForTheBooking()
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        DateTimeOffset now = _factory.Services.GetRequiredService<IClock>().UtcNow;

        // Act
        (HttpStatusCode status, string text) = await SignInRawAsync(booking.Id, $"  {email.ToUpperInvariant()} ");
        GuestSessionDto session = ValueOf<GuestSessionDto>(text);

        // Assert
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal(booking.Id, session.BookingId);
        Assert.InRange(session.ExpiresAt, now.AddHours(2).AddMinutes(-1), now.AddHours(2).AddMinutes(1));
    }

    /// <summary>
    /// Security (criterion 3): an unknown number, a cancelled booking and a guest token that is expired, tampered, signed
    /// with another key, unsigned or a staff token all get the status and the text of a wrong email, to the character.
    /// </summary>
    /// <param name="refusal">The kind of failed sign-in.</param>
    [Theory]
    [InlineData("unknown number")]
    [InlineData("cancelled booking")]
    [InlineData("expired token")]
    [InlineData("tampered token")]
    [InlineData("token signed with another key")]
    [InlineData("unsigned token")]
    [InlineData("staff token as guest token")]
    public async Task Refusal_EveryKindOfFailedSignIn_HasTheStatusAndTextOfAWrongEmail(string refusal)
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        (Booking other, string otherEmail) = SeedBooking();
        (HttpStatusCode status, string text) wrongEmail = await SignInRawAsync(booking.Id, "someone.else@example.com");
        string bookingPath = $"api/bookings/{booking.Id}";

        // Act
        (HttpStatusCode status, string text) answer = refusal switch
        {
            "unknown number" => await SignInRawAsync(other.Id + 100_000, otherEmail),
            "cancelled booking" => await SignInCancelledAsync(other, otherEmail),
            "expired token" => await SendAsync(HttpMethod.Get, bookingPath, ExpiredToken(booking.Id)),
            "tampered token" => await SendAsync(HttpMethod.Get, $"api/bookings/{other.Id}", Tampered(await SignInAsync(booking.Id, email), other.Id)),
            "token signed with another key" => await SendAsync(HttpMethod.Get, bookingPath, TokenWithAnotherKey(booking.Id)),
            "unsigned token" => await SendAsync(HttpMethod.Get, bookingPath, UnsignedToken(booking.Id)),
            "staff token as guest token" => await SendAsync(HttpMethod.Get, bookingPath, _factory.IssueStaffToken(StaffRole.Manager)),
            _ => throw new ArgumentOutOfRangeException(nameof(refusal), refusal, "Unknown refusal."),
        };

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, wrongEmail.status);
        Assert.Contains(ErrorCodes.Api.GuestUnauthorized, wrongEmail.text, StringComparison.Ordinal);
        Assert.Equal(wrongEmail, answer);
    }

    /// <summary>
    /// Boundary (criterion 3): five failed sign-ins for one number get the refusal; the sixth within 15 minutes gets 429,
    /// even with the right email.
    /// </summary>
    [Fact]
    public async Task SignIn_SixthAttemptAfterFiveFailures_ReturnsTooManyRequests()
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        List<HttpStatusCode> failures = [];
        for (int attempt = 0; attempt < MaxFailures; attempt++)
        {
            (HttpStatusCode status, _) = await SignInRawAsync(booking.Id, $"guess-{attempt}@example.com");
            failures.Add(status);
        }

        // Act
        (HttpStatusCode sixth, string text) = await SignInRawAsync(booking.Id, email);

        // Assert
        Assert.All(failures, status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, sixth);
        Assert.Contains(ErrorCodes.Booking.TooManyLookups, text, StringComparison.Ordinal);
    }

    #endregion

    #region Criterion 4: no passport number, no staff endpoint

    /// <summary>
    /// Security (criterion 4): no answer to a guest token holds the passport number of the guest, not the sign-in, the
    /// booking, the guest, the move or the cancel.
    /// </summary>
    [Fact]
    public async Task GuestAnswers_GuestWithAPassportNumber_NeverContainTheNumber()
    {
        // Arrange
        (Booking booking, string email) = SeedBooking(PassportNumber);
        (HttpStatusCode signInStatus, string signInText) = await SignInRawAsync(booking.Id, email);
        string token = ValueOf<GuestSessionDto>(signInText).Token;

        // Act
        (HttpStatusCode status, string text)[] answers =
        [
            (signInStatus, signInText),
            await RunAsync(ReadBooking, booking, token),
            await RunAsync(ReadGuest, booking, token),
            await RunAsync(Reschedule, booking, token),
            await RunAsync(Cancel, booking, token),
        ];

        // Assert
        Assert.All(answers, answer => Assert.Equal(HttpStatusCode.OK, answer.status));
        Assert.All(answers, answer => Assert.DoesNotContain(PassportNumber, answer.text, StringComparison.OrdinalIgnoreCase));
        Assert.Contains("\"passportNumber\":null", answers[2].text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Security (criterion 4): a guest token on any endpoint that names no guest permission gets 403 and never reaches
    /// the use case. One case per endpoint, read from the controllers, so a new staff endpoint is covered without a change
    /// here.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The address, with a value for each route parameter.</param>
    [Theory]
    [MemberData(nameof(StaffOnlyEndpoints))]
    public async Task StaffEndpoint_GuestToken_ReturnsForbidden(string method, string path)
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        string token = await SignInAsync(booking.Id, email);
        object? body = method is "POST" or "PUT" or "PATCH" ? new { } : null;

        // Act
        (HttpStatusCode status, string text) = await SendAsync(new HttpMethod(method), path, token, body);

        // Assert
        Assert.True(status == HttpStatusCode.Forbidden, $"{method} {path}: {(int)status} {text}");
        Assert.Contains(ErrorCodes.Api.Forbidden, text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Security: exactly the four guest operations name a guest permission, so no other endpoint opens to a guest
    /// unnoticed, and the list of staff endpoints above is not empty.
    /// </summary>
    [Fact]
    public void GuestEndpoints_EveryActionWithAGuestPermission_IsOneOfTheFourGuestOperations()
    {
        // Act
        string[] guestActions = ActionsWithPermission()
            .Where(action => action.Permission.GuestPermission is not null)
            .Select(action => $"{action.Action.DeclaringType?.Name}.{action.Action.Name}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Assert
        Assert.Equal(
            ["BookingsController.CancelAsync", "BookingsController.GetByIdAsync", "BookingsController.RescheduleAsync", "GuestsController.GetByIdAsync"],
            guestActions);
        Assert.NotEmpty(StaffOnlyEndpoints());
    }

    /// <summary>Security: a guest token in the staff header is no staff token; it gives no staff rights and no guest rights.</summary>
    [Fact]
    public async Task StaffHeader_GuestToken_ReturnsUnauthorized()
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        string token = await SignInAsync(booking.Id, email);
        using HttpClient client = _factory.CreateServiceClient();
        client.DefaultRequestHeaders.Add(ApiAuthentication.StaffTokenHeader, token);

        // Act
        using HttpResponseMessage own = await client.GetAsync($"api/bookings/{booking.Id}", TestContext.Current.CancellationToken);
        using HttpResponseMessage list = await client.GetAsync("api/bookings/availability", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, own.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
    }

    /// <summary>
    /// Security (criterion 3): a token signed with the right key for the guest audience must also name the role Guest
    /// and a booking. The same token with both opens the booking, so the two refusals are for the claims alone.
    /// </summary>
    [Fact]
    public async Task GuestToken_SignedWithoutTheRoleGuestOrABooking_IsRefused()
    {
        // Arrange
        (Booking booking, _) = SeedBooking();
        string path = $"api/bookings/{booking.Id}";
        string bookingId = booking.Id.ToString(CultureInfo.InvariantCulture);

        // Act
        (HttpStatusCode status, string text) guest = await SendAsync(HttpMethod.Get, path, SignedToken(Roles.Guest, bookingId));
        (HttpStatusCode status, string text) otherRole = await SendAsync(HttpMethod.Get, path, SignedToken(Roles.Manager, bookingId));
        (HttpStatusCode status, string text) noBooking = await SendAsync(HttpMethod.Get, path, SignedToken(Roles.Guest, "0"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, guest.status);
        Assert.Equal(HttpStatusCode.Unauthorized, otherRole.status);
        Assert.Contains(ErrorCodes.Api.GuestUnauthorized, otherRole.text, StringComparison.Ordinal);
        Assert.Equal(otherRole, noBooking);
    }

    /// <summary>
    /// Security: a call with a valid staff token and a valid guest token is a staff call. The guest token is not read,
    /// so it neither limits the staff to the booking of the guest nor adds to their rights.
    /// </summary>
    [Fact]
    public async Task StaffAndGuestToken_BothValid_TheStaffTokenDecides()
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        (Booking other, _) = SeedBooking();
        string guestToken = await SignInAsync(booking.Id, email);
        using HttpClient client = _factory.CreateStaffClient(StaffRole.Manager);
        client.DefaultRequestHeaders.Add(ApiAuthentication.GuestTokenHeader, guestToken);

        // Act
        using HttpResponseMessage otherBooking = await client.GetAsync($"api/bookings/{other.Id}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, otherBooking.StatusCode);
    }

    #endregion

    #region Helpers

    /// <summary>Every endpoint that requires a permission and names no guest permission, with a value for each route parameter.</summary>
    /// <returns>Pairs of HTTP method and address.</returns>
    public static TheoryData<string, string> StaffOnlyEndpoints()
    {
        TheoryData<string, string> endpoints = [];
        foreach ((MethodInfo action, RequiresPermissionAttribute permission) in ActionsWithPermission())
        {
            HttpMethodAttribute? verb = action.GetCustomAttributes<HttpMethodAttribute>().FirstOrDefault();
            if (permission.GuestPermission is not null || verb is null)
            {
                continue;
            }

            string? prefix = action.DeclaringType?.GetCustomAttribute<RouteAttribute>()?.Template;
            string template = string.Join('/', new[] { prefix, verb.Template }.Where(part => !string.IsNullOrEmpty(part)));
            endpoints.Add(verb.HttpMethods.First(), string.Join('/', template.Split('/').Select(FillParameter)));
        }

        return endpoints;
    }

    private static IEnumerable<(MethodInfo Action, RequiresPermissionAttribute Permission)> ActionsWithPermission() =>
        typeof(Program).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Select(action => (Action: action, Permission: action.GetCustomAttribute<RequiresPermissionAttribute>()))
            .Where(entry => entry.Permission is not null)
            .Select(entry => (entry.Action, Permission: entry.Permission ?? throw new InvalidOperationException("Filtered above.")));

    /// <summary>A value that meets the constraint of a route parameter; any other segment stays as it is.</summary>
    private static string FillParameter(string segment) =>
        !segment.StartsWith('{') ? segment
        : segment.Contains(":int", StringComparison.Ordinal) ? "1"
        : segment.Contains(":guid", StringComparison.Ordinal) ? "00000000-0000-0000-0000-000000000001"
        : "x";

    /// <summary>A pending booking in a room of its own, with a guest whose email is unique to it.</summary>
    private (Booking Booking, string Email) SeedBooking(string? passportNumber = null)
    {
        ServiceWorld world = _factory.World;
        Room room = world.AddRoom(NextRoomNumber());
        string email = $"guest-{room.RoomNumber}@example.com";
        Guest guest = world.AddGuest("Anna", "Jensen", email, passportNumber);
        Booking booking = world.AddBooking(room.Id, guest.Id, startOffsetDays: 5, nights: 2);
        return (booking, email);
    }

    private string NextRoomNumber() => (800 + _factory.World.Rooms.Items.Count).ToString(CultureInfo.InvariantCulture);

    /// <summary>Signs in with the number and the email, and gives the guest token; the sign-in must succeed.</summary>
    private async Task<string> SignInAsync(int bookingId, string email)
    {
        (HttpStatusCode status, string text) = await SignInRawAsync(bookingId, email);
        Assert.True(status == HttpStatusCode.OK, $"The sign-in failed: {(int)status} {text}");
        return ValueOf<GuestSessionDto>(text).Token;
    }

    private async Task<(HttpStatusCode Status, string Text)> SignInRawAsync(int bookingId, string email)
    {
        using HttpClient client = _factory.CreateServiceClient();
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            GuestLoginPath,
            new { bookingId, email },
            ApiJson.Options,
            TestContext.Current.CancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private async Task<(HttpStatusCode Status, string Text)> SignInCancelledAsync(Booking booking, string email)
    {
        booking.Cancel();
        return await SignInRawAsync(booking.Id, email);
    }

    /// <summary>Runs one guest operation on a booking with the token of the web application and, if given, a guest token.</summary>
    private Task<(HttpStatusCode Status, string Text)> RunAsync(string operation, Booking booking, string? guestToken) =>
        operation switch
        {
            ReadBooking => SendAsync(HttpMethod.Get, $"api/bookings/{booking.Id}", guestToken),
            ReadGuest => SendAsync(HttpMethod.Get, $"api/guests/{booking.GuestId}", guestToken),
            Cancel => SendAsync(HttpMethod.Post, $"api/bookings/{booking.Id}/cancel", guestToken),
            Reschedule => SendAsync(
                HttpMethod.Post,
                ReschedulePath,
                guestToken,
                new RescheduleBookingRequest(booking.Id, booking.StartDate.AddDays(10), booking.EndDate.AddDays(10), booking.RoomId)),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown operation."),
        };

    private async Task<(HttpStatusCode Status, string Text)> SendAsync(HttpMethod method, string path, string? guestToken, object? body = null)
    {
        using HttpClient client = _factory.CreateServiceClient();
        using HttpRequestMessage request = new(method, path);
        if (guestToken is not null)
        {
            request.Headers.Add(ApiAuthentication.GuestTokenHeader, guestToken);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: ApiJson.Options);
        }

        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private static TValue ValueOf<TValue>(string text)
    {
        ApiResponse<TValue>? body = JsonSerializer.Deserialize<ApiResponse<TValue>>(text, ApiJson.Options);
        Assert.NotNull(body);
        Assert.NotNull(body.Value);
        return body.Value;
    }

    /// <summary>A guest token signed with the right key, two hours from 1 January 2020, so long expired.</summary>
    private static string ExpiredToken(int bookingId) =>
        new JwtGuestTokenIssuer(new StaffSecurityOptions { SigningKey = ApiFactory.SigningKey }, new FakeClock { Today = new DateOnly(2020, 1, 1) })
            .Issue(bookingId)
            .Token;

    /// <summary>A guest token that is valid now but signed with a key the API does not hold.</summary>
    private string TokenWithAnotherKey(int bookingId) =>
        new JwtGuestTokenIssuer(new StaffSecurityOptions { SigningKey = OtherSigningKey }, _factory.Services.GetRequiredService<IClock>())
            .Issue(bookingId)
            .Token;

    /// <summary>A token signed with the right key for the guest audience, valid now, with the role and booking given.</summary>
    private string SignedToken(string role, string bookingId)
    {
        DateTimeOffset now = _factory.Services.GetRequiredService<IClock>().UtcNow;
        SecurityTokenDescriptor descriptor = new()
        {
            Issuer = StaffSecurityOptions.Issuer,
            Audience = JwtGuestTokenIssuer.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.Add(JwtGuestTokenIssuer.Lifetime).UtcDateTime,
            SigningCredentials = new SigningCredentials(
                JwtStaffTokenIssuer.SigningKeyFor(new StaffSecurityOptions { SigningKey = ApiFactory.SigningKey }),
                SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtStaffTokenIssuer.RoleClaim] = role,
                [JwtGuestTokenIssuer.BookingClaim] = bookingId,
            },
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <summary>A guest token for one booking whose booking claim is changed to another, with the old signature.</summary>
    private static string Tampered(string token, int otherBookingId)
    {
        string[] parts = token.Split('.');
        using JsonDocument payload = JsonDocument.Parse(Base64UrlEncoder.Decode(parts[1]));
        Dictionary<string, object?> claims = payload.RootElement.EnumerateObject()
            .ToDictionary(claim => claim.Name, claim => (object?)claim.Value.Clone());
        claims[JwtGuestTokenIssuer.BookingClaim] = otherBookingId.ToString(CultureInfo.InvariantCulture);
        parts[1] = Base64UrlEncoder.Encode(JsonSerializer.Serialize(claims));
        return string.Join('.', parts);
    }

    /// <summary>A token with the claims of a guest and no signature at all (<c>alg: none</c>).</summary>
    private static string UnsignedToken(int bookingId)
    {
        string header = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""));
        string payload = Base64UrlEncoder.Encode(Encoding.UTF8.GetBytes(
            $$"""{"iss":"nfhotel-api","aud":"nfhotel-guest","role":"Guest","booking":"{{bookingId}}","exp":4102444800}"""));
        return $"{header}.{payload}.";
    }

    #endregion
}
