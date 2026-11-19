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
/// The guest sign-in and the guest's own booking over HTTP (MIL-018, ADR-0047, and since MIL-085 ADR-0049), through the
/// real pipeline of the API: the staff token, the guest token, the attributes of the staff and the guest routes, the
/// limits of the guest sign-in and the use cases.
/// </summary>
/// <remarks>
/// The addresses and bodies are written out instead of taken from the contracts, as in
/// <see cref="BookingReadProtectionTests"/>, so the tests pin what the web application depends on. Each test calls from
/// an address of its own, so the limit per address of one test does not reach another.
/// </remarks>
public sealed class GuestAccessTests : IClassFixture<ApiFactory>
{
    private const string GuestLoginPath = "api/auth/guest-login";
    private const string PublicBookingPath = "api/bookings/public";
    private const string GuestBookingPath = "api/guest/booking";
    private const string GuestCancelPath = "api/guest/booking/cancel";
    private const string GuestReschedulePath = "api/guest/booking/reschedule";
    private const string StaffReschedulePath = "api/bookings/reschedule";
    private const string OtherSigningKey = "another-signing-key-0123456789-another-key";
    private const string PassportNumber = "P9876543";
    private const int MaxFailuresPerReference = 5;
    private const int MaxFailuresPerAddress = 20;

    private const string ReadBooking = "read the own booking";
    private const string Cancel = "cancel the own booking";
    private const string Reschedule = "move the own booking to other dates";

    private static int s_nextAddress;

    private readonly ApiFactory _factory;
    private readonly string _address = NextAddress();

    public GuestAccessTests(ApiFactory factory)
    {
        _factory = factory;
    }

    #region No guest token

    /// <summary>
    /// Negative: without a guest token, the three guest operations get the one refusal of a guest, and the booking stays
    /// as it was.
    /// </summary>
    /// <param name="operation">The operation.</param>
    [Theory]
    [InlineData(ReadBooking)]
    [InlineData(Cancel)]
    [InlineData(Reschedule)]
    public async Task GuestOperation_NoGuestToken_ReturnsTheRefusalOfAGuest(string operation)
    {
        // Arrange
        (Booking booking, _) = SeedBooking();

        // Act
        (HttpStatusCode status, string text) = await RunAsync(operation, booking, guestToken: null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, status);
        Assert.Contains(ErrorCodes.Api.GuestUnauthorized, text, StringComparison.Ordinal);
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    /// <summary>
    /// Positive (ADR-0049 decisions 3 and 7, MIL-085 criteria 3 and 5): a new booking needs no token, and its answer is the
    /// receipt with a reference in the form of a version 4 UUID and a guest session that opens exactly that booking;
    /// nothing in it names the key of the booking, the guest or the passport number.
    /// </summary>
    [Fact]
    public async Task PublicBooking_NoToken_ReturnsTheReceiptWithAReferenceAndASessionThatOpensIt()
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
        using HttpResponseMessage created = await client.PostAsJsonAsync(PublicBookingPath, request, ApiJson.Options, TestContext.Current.CancellationToken);
        string createdText = await created.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        PublicBookingDto booked = ValueOf<PublicBookingDto>(createdText);
        (HttpStatusCode ownStatus, string ownText) = await SendAsync(HttpMethod.Get, GuestBookingPath, booked.Session.Token);

        // Assert
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        Booking stored = world.Bookings.Items.Single(booking => booking.Reference == booked.Receipt.Reference);
        Assert.Equal(4, booked.Receipt.Reference.Version);
        Assert.Matches(UuidOfVersion4, ValueText(createdText, "receipt", "reference"));
        Assert.Equal(new BookingReceiptDto(stored.Reference, checkIn, checkIn.AddDays(2), 2, RoomSize.Suite, BookingStatus.Pending), booked.Receipt);
        Assert.False(string.IsNullOrWhiteSpace(booked.Session.Token));
        Assert.Equal(HttpStatusCode.OK, ownStatus);
        Assert.Equal(booked.Receipt, ValueOf<BookingReceiptDto>(ownText));
        AssertNoKeyNoGuestAndNoPassport(createdText, stored);
    }

    #endregion

    #region The own booking only

    /// <summary>Positive: the guest token of a booking reads, cancels and moves that booking through the guest routes.</summary>
    /// <param name="operation">The operation.</param>
    [Theory]
    [InlineData(ReadBooking)]
    [InlineData(Cancel)]
    [InlineData(Reschedule)]
    public async Task GuestOperation_OwnBooking_IsAllowed(string operation)
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        string token = await SignInAsync(booking, email);

        // Act
        (HttpStatusCode status, string text) = await RunAsync(operation, booking, token);

        // Assert
        Assert.True(status == HttpStatusCode.OK, $"{operation}: {(int)status} {text}");
        Assert.Equal(booking.Reference, ValueOf<BookingReceiptDto>(text).Reference);
    }

    /// <summary>
    /// Security (ADR-0049 decision 3, MIL-085 criterion 5): the staff routes no longer admit a guest token. The guest
    /// token of booking A gets 403 on booking B and on A itself, on their guests, and on the cancel and the move of B;
    /// nothing changes.
    /// </summary>
    /// <param name="target">Which booking the staff route is asked for.</param>
    /// <param name="operation">The staff route.</param>
    [Theory]
    [InlineData("other", "read")]
    [InlineData("other", "read guest")]
    [InlineData("other", "cancel")]
    [InlineData("other", "reschedule")]
    [InlineData("own", "read")]
    [InlineData("own", "read guest")]
    [InlineData("own", "cancel")]
    [InlineData("own", "reschedule")]
    public async Task StaffBookingRoute_GuestToken_ReturnsForbidden(string target, string operation)
    {
        // Arrange
        (Booking own, string email) = SeedBooking();
        (Booking other, _) = SeedBooking();
        string token = await SignInAsync(own, email);
        Booking booking = target == "own" ? own : other;
        DateOnly startBefore = booking.StartDate;

        // Act
        (HttpStatusCode status, string text) = operation switch
        {
            "read" => await SendAsync(HttpMethod.Get, $"api/bookings/{booking.Id}", token),
            "read guest" => await SendAsync(HttpMethod.Get, $"api/guests/{booking.GuestId}", token),
            "cancel" => await SendAsync(HttpMethod.Post, $"api/bookings/{booking.Id}/cancel", token),
            _ => await SendAsync(
                HttpMethod.Post,
                StaffReschedulePath,
                token,
                new RescheduleBookingRequest(booking.Id, booking.StartDate.AddDays(10), booking.EndDate.AddDays(10), booking.RoomId)),
        };

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, status);
        Assert.Contains(ErrorCodes.Api.Forbidden, text, StringComparison.Ordinal);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(startBefore, booking.StartDate);
    }

    /// <summary>Security: a staff token on a guest route gets 403; staff work with the staff routes.</summary>
    [Fact]
    public async Task GuestRoute_StaffToken_ReturnsForbidden()
    {
        // Arrange
        using HttpClient client = _factory.CreateStaffClient(StaffRole.Manager);

        // Act
        using HttpResponseMessage response = await client.GetAsync(GuestBookingPath, TestContext.Current.CancellationToken);
        string text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains(ErrorCodes.Api.Forbidden, text, StringComparison.Ordinal);
    }

    /// <summary>Security: a guest moves the own booking to other dates in its room; the request has no room to name (UC-010).</summary>
    [Fact]
    public async Task Reschedule_GuestRoute_KeepsTheRoomOfTheBooking()
    {
        // Arrange
        (Booking own, string email) = SeedBooking();
        string token = await SignInAsync(own, email);
        int roomBefore = own.RoomId;
        DateOnly checkIn = own.StartDate.AddDays(30);

        // Act
        (HttpStatusCode status, string text) = await SendAsync(
            HttpMethod.Post,
            GuestReschedulePath,
            token,
            new { checkInDate = checkIn, checkOutDate = checkIn.AddDays(3), roomId = 1 });

        // Assert
        Assert.True(status == HttpStatusCode.OK, $"{(int)status} {text}");
        Assert.Equal(roomBefore, own.RoomId);
        Assert.Equal(checkIn, own.StartDate);
    }

    #endregion

    #region One refusal, and the limits

    /// <summary>
    /// Positive (MIL-085 criterion 4): the reference in upper case and the email in another letter case, both with spaces
    /// around them, give a session of two hours that names no booking.
    /// </summary>
    [Fact]
    public async Task SignIn_RightReferenceAndEmailInAnotherCase_ReturnsASessionOfTwoHours()
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        DateTimeOffset now = _factory.Services.GetRequiredService<IClock>().UtcNow;

        // Act
        (HttpStatusCode status, string text) = await SignInRawAsync(
            $" {BookingReference.Format(booking.Reference).ToUpperInvariant()} ",
            $"  {email.ToUpperInvariant()} ");
        GuestSessionDto session = ValueOf<GuestSessionDto>(text);

        // Assert
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.InRange(session.ExpiresAt, now.AddHours(2).AddMinutes(-1), now.AddHours(2).AddMinutes(1));
        Assert.DoesNotContain("bookingId", text, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Security (ADR-0049 decisions 2 and 5, MIL-085 criterion 4): an unknown reference, an unreadable one, a booking
    /// number of the old kind, the key of the booking, a cancelled booking, and a guest token that is expired, tampered,
    /// signed with another key, unsigned or a staff token all get the status and the text of a wrong email, to the
    /// character.
    /// </summary>
    /// <param name="refusal">The kind of failed sign-in.</param>
    [Theory]
    [InlineData("unknown reference")]
    [InlineData("unreadable reference")]
    [InlineData("old booking number")]
    [InlineData("the key of the booking")]
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
        (HttpStatusCode status, string text) wrongEmail = await SignInRawAsync(BookingReference.Format(booking.Reference), "someone.else@example.com");

        // Act
        (HttpStatusCode status, string text) answer = refusal switch
        {
            "unknown reference" => await SignInRawAsync(BookingReference.Format(Guid.NewGuid()), otherEmail),
            "unreadable reference" => await SignInRawAsync("not a reference", otherEmail),
            "old booking number" => await SignInRawAsync($"FLZ-{other.Id:D6}", otherEmail),
            "the key of the booking" => await SignInRawAsync(other.Id.ToString(CultureInfo.InvariantCulture), otherEmail),
            "cancelled booking" => await SignInCancelledAsync(other, otherEmail),
            "expired token" => await SendAsync(HttpMethod.Get, GuestBookingPath, ExpiredToken(booking.Id)),
            "tampered token" => await SendAsync(HttpMethod.Get, GuestBookingPath, Tampered(await SignInAsync(booking, email), other.Id)),
            "token signed with another key" => await SendAsync(HttpMethod.Get, GuestBookingPath, TokenWithAnotherKey(booking.Id)),
            "unsigned token" => await SendAsync(HttpMethod.Get, GuestBookingPath, UnsignedToken(booking.Id)),
            "staff token as guest token" => await SendAsync(HttpMethod.Get, GuestBookingPath, _factory.IssueStaffToken(StaffRole.Manager)),
            _ => throw new ArgumentOutOfRangeException(nameof(refusal), refusal, "Unknown refusal."),
        };

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, wrongEmail.status);
        Assert.Contains(ErrorCodes.Api.GuestUnauthorized, wrongEmail.text, StringComparison.Ordinal);
        Assert.Equal(wrongEmail, answer);
    }

    /// <summary>
    /// Robustness (MIL-085 criterion 6): references that are hostile, very long or not text of the usual kind get the one
    /// refusal and never an answer of the 500 kind.
    /// </summary>
    /// <param name="reference">The reference that is tried.</param>
    [Theory]
    [InlineData("' OR '1'='1")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("%00")]
    public async Task SignIn_HostileReference_GetsTheOneRefusal(string reference)
    {
        // Act
        (HttpStatusCode status, string text) = await SignInRawAsync(reference, "anna@example.com");
        (HttpStatusCode longStatus, _) = await SignInRawAsync(new string('9', 10_000), "anna@example.com");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, status);
        Assert.Contains(ErrorCodes.Api.GuestUnauthorized, text, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.Unauthorized, longStatus);
    }

    /// <summary>
    /// Robustness (MIL-077, ADR-0049 decision 2, review RC-418): a reference with a control or a direction character is
    /// refused by the input rules of every request before the sign-in runs, with the 400 of any hostile input, which
    /// names no booking; nothing is read and no try is counted.
    /// </summary>
    /// <param name="character">The character at the end of a reference of the usual form.</param>
    [Theory]
    [InlineData(0x0000)]
    [InlineData(0x202E)]
    public async Task SignIn_ReferenceWithAControlOrDirectionCharacter_GetsTheRefusalOfHostileInputAndCountsNothing(int character)
    {
        // Arrange
        InMemoryGuestSignInLimits limits = Limits();
        int addresses = limits.TrackedAddressCount;
        string reference = "3f2504e0-4f89-41d3-9a0c-0305e82c3301" + (char)character;

        // Act
        (HttpStatusCode status, string text) = await SignInRawAsync(reference, "anna@example.com", NextAddress());

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Contains("api.invalid_request", text, StringComparison.Ordinal);
        Assert.Equal(addresses, limits.TrackedAddressCount);
    }

    /// <summary>
    /// Robustness (MIL-077, ADR-0049 decision 2, review RC-418): a reference that is no text cannot be bound and gets the
    /// same 400 as any request that cannot be bound; nothing is read and no try is counted.
    /// </summary>
    [Fact]
    public async Task SignIn_ReferenceThatIsNoText_GetsTheRefusalOfHostileInputAndCountsNothing()
    {
        // Arrange
        InMemoryGuestSignInLimits limits = Limits();
        int addresses = limits.TrackedAddressCount;
        using HttpClient client = _factory.CreateServiceClient();
        using HttpRequestMessage request = new(HttpMethod.Post, GuestLoginPath)
        {
            Content = new StringContent("""{"reference":123,"email":"anna@example.com"}""", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(ApiAuthentication.ClientAddressHeader, NextAddress());

        // Act
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        string text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("api.invalid_request", text, StringComparison.Ordinal);
        Assert.Equal(addresses, limits.TrackedAddressCount);
    }

    /// <summary>
    /// Boundary (ADR-0049 decision 6, MIL-085 criterion 4): five failed sign-ins for one reference get the refusal; the
    /// sixth within 15 minutes gets 429, even with the right email, and from another address too.
    /// </summary>
    [Fact]
    public async Task SignIn_SixthTryForOneReference_ReturnsTooManyRequests()
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        string reference = BookingReference.Format(booking.Reference);
        List<HttpStatusCode> failures = [];
        for (int attempt = 0; attempt < MaxFailuresPerReference; attempt++)
        {
            (HttpStatusCode status, _) = await SignInRawAsync(reference, $"guess-{attempt}@example.com");
            failures.Add(status);
        }

        // Act
        (HttpStatusCode sixth, string text) = await SignInRawAsync(reference, email);
        (HttpStatusCode fromAnotherAddress, _) = await SignInRawAsync(reference, email, NextAddress());

        // Assert
        Assert.All(failures, status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, sixth);
        Assert.Contains(ErrorCodes.Booking.TooManyLookups, text, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.TooManyRequests, fromAnotherAddress);
    }

    /// <summary>
    /// Boundary (ADR-0049 decision 6, MIL-085 criterion 4): one address that tries an email against 20 references gets the
    /// refusal 20 times and 429 at the 21st try, even for a booking with its right email; another address still gets in.
    /// </summary>
    [Fact]
    public async Task SignIn_TwentyFirstFailedTryFromOneAddress_ReturnsTooManyRequests()
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        List<HttpStatusCode> failures = [];
        for (int attempt = 0; attempt < MaxFailuresPerAddress; attempt++)
        {
            (HttpStatusCode status, _) = await SignInRawAsync(BookingReference.Format(Guid.NewGuid()), email);
            failures.Add(status);
        }

        // Act
        (HttpStatusCode next, string text) = await SignInRawAsync(BookingReference.Format(booking.Reference), email);
        (HttpStatusCode fromAnotherAddress, _) = await SignInRawAsync(BookingReference.Format(booking.Reference), email, NextAddress());

        // Assert
        Assert.All(failures, status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, next);
        Assert.Contains(ErrorCodes.Booking.TooManyLookups, text, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, fromAnotherAddress);
    }

    /// <summary>
    /// Boundary (ADR-0049 decision 6): unreadable references, the old booking numbers among them, count against the
    /// address too, so one caller cannot try old numbers without a limit.
    /// </summary>
    [Fact]
    public async Task SignIn_TwentyUnreadableReferencesFromOneAddress_StopsTheAddress()
    {
        // Arrange
        List<HttpStatusCode> failures = [];
        for (int attempt = 0; attempt < MaxFailuresPerAddress; attempt++)
        {
            (HttpStatusCode status, _) = await SignInRawAsync($"FLZ-{attempt + 1:D6}", "anna@example.com");
            failures.Add(status);
        }

        // Act
        (HttpStatusCode next, _) = await SignInRawAsync("FLZ-000099", "anna@example.com");

        // Assert
        Assert.All(failures, status => Assert.Equal(HttpStatusCode.Unauthorized, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, next);
    }

    #endregion

    #region What a guest sees, and no staff endpoint

    /// <summary>
    /// Security (ADR-0049 decision 7, MIL-085 criterion 5): every answer to a guest holds the receipt and nothing more:
    /// no key of the booking, no guest, no passport number; the sign-in holds the token and its expiry alone.
    /// </summary>
    [Fact]
    public async Task GuestAnswers_GuestWithAPassportNumber_HoldTheReceiptAndNothingMore()
    {
        // Arrange
        (Booking booking, string email) = SeedBooking(PassportNumber);
        (HttpStatusCode signInStatus, string signInText) = await SignInRawAsync(BookingReference.Format(booking.Reference), email);
        string token = ValueOf<GuestSessionDto>(signInText).Token;

        // Act
        (HttpStatusCode status, string text)[] answers =
        [
            await RunAsync(ReadBooking, booking, token),
            await RunAsync(Reschedule, booking, token),
            await RunAsync(Cancel, booking, token),
        ];

        // Assert
        Assert.Equal(HttpStatusCode.OK, signInStatus);
        Assert.Equal(["expiresAt", "token"], PropertyNamesOf(signInText));
        Assert.All(answers, answer => Assert.Equal(HttpStatusCode.OK, answer.status));
        Assert.All(answers, answer => Assert.Equal(["endDate", "nights", "reference", "roomSize", "startDate", "status"], PropertyNamesOf(answer.text)));
        Assert.All(answers, answer => AssertNoKeyNoGuestAndNoPassport(answer.text, booking));
    }

    /// <summary>
    /// Security (ADR-0049 decision 3): a guest token on any endpoint that requires a permission of staff gets 403 and never
    /// reaches the use case. One case per endpoint, read from the controllers, so a new staff endpoint is covered without
    /// a change here.
    /// </summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The address, with a value for each route parameter.</param>
    [Theory]
    [MemberData(nameof(StaffEndpoints))]
    public async Task StaffEndpoint_GuestToken_ReturnsForbidden(string method, string path)
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        string token = await SignInAsync(booking, email);
        object? body = method is "POST" or "PUT" or "PATCH" ? new { } : null;

        // Act
        (HttpStatusCode status, string text) = await SendAsync(new HttpMethod(method), path, token, body);

        // Assert
        Assert.True(status == HttpStatusCode.Forbidden, $"{method} {path}: {(int)status} {text}");
        Assert.Contains(ErrorCodes.Api.Forbidden, text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Security (ADR-0049 decision 3): exactly the three actions of the guest routes are open to a guest, none of them
    /// takes a key, and the list of staff endpoints above is not empty.
    /// </summary>
    [Fact]
    public void GuestEndpoints_TheThreeGuestRoutes_AreTheOnlyOnesOpenToAGuestAndTakeNoKey()
    {
        // Act
        MethodInfo[] guestActions = Controllers()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(action => action.GetCustomAttribute<RequiresGuestAttribute>() is not null)
            .ToArray();
        string[] names = guestActions
            .Select(action => $"{action.DeclaringType?.Name}.{action.Name}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Assert
        Assert.Equal(["GuestBookingController.CancelAsync", "GuestBookingController.GetAsync", "GuestBookingController.RescheduleAsync"], names);
        Assert.All(guestActions, action => Assert.DoesNotContain(action.GetParameters(), parameter => parameter.ParameterType == typeof(int)));
        Assert.NotEmpty(StaffEndpoints());
    }

    /// <summary>
    /// Security (ADR-0049 decision 3): each guest route names the permission that the staff routes held for a guest before
    /// (MIL-018), the read the permission to see the own data and the two changes the permission to edit the own booking,
    /// and the Guest role holds each of them, as the Access Control Matrix says.
    /// </summary>
    [Fact]
    public void GuestEndpoints_EachRoute_NamesAPermissionOfTheGuestRole()
    {
        // Act
        Dictionary<string, string> permissions = typeof(NFHotel.Api.Controllers.GuestBookingController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Select(action => (action.Name, Guest: action.GetCustomAttribute<RequiresGuestAttribute>()))
            .Where(entry => entry.Guest is not null)
            .ToDictionary(entry => entry.Name, entry => entry.Guest!.Permission, StringComparer.Ordinal);

        // Assert
        Assert.Equal(Permissions.ViewOwnGuestData, permissions["GetAsync"]);
        Assert.Equal(Permissions.EditOwnBooking, permissions["CancelAsync"]);
        Assert.Equal(Permissions.EditOwnBooking, permissions["RescheduleAsync"]);
        Assert.All(permissions.Values, permission => Assert.True(RolePermissions.HasPermission(Roles.Guest, permission), permission));
    }

    /// <summary>Security: a guest token in the staff header is no staff token; it gives no staff rights and no guest rights.</summary>
    [Fact]
    public async Task StaffHeader_GuestToken_ReturnsUnauthorized()
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        string token = await SignInAsync(booking, email);
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
    /// Security: a token signed with the right key for the guest audience must also name the role Guest and a booking.
    /// The same token with both opens the booking, so the two refusals are for the claims alone.
    /// </summary>
    [Fact]
    public async Task GuestToken_SignedWithoutTheRoleGuestOrABooking_IsRefused()
    {
        // Arrange
        (Booking booking, _) = SeedBooking();
        string bookingId = booking.Id.ToString(CultureInfo.InvariantCulture);

        // Act
        (HttpStatusCode status, string text) guest = await SendAsync(HttpMethod.Get, GuestBookingPath, SignedToken(Roles.Guest, bookingId));
        (HttpStatusCode status, string text) otherRole = await SendAsync(HttpMethod.Get, GuestBookingPath, SignedToken(Roles.Manager, bookingId));
        (HttpStatusCode status, string text) noBooking = await SendAsync(HttpMethod.Get, GuestBookingPath, SignedToken(Roles.Guest, "0"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, guest.status);
        Assert.Equal(HttpStatusCode.Unauthorized, otherRole.status);
        Assert.Contains(ErrorCodes.Api.GuestUnauthorized, otherRole.text, StringComparison.Ordinal);
        Assert.Equal(otherRole, noBooking);
    }

    /// <summary>
    /// Security: a call with a valid staff token and a valid guest token is a staff call. The guest token is not read, so
    /// it neither limits the staff to the booking of the guest nor adds to their rights, and the guest routes refuse it.
    /// </summary>
    [Fact]
    public async Task StaffAndGuestToken_BothValid_TheStaffTokenDecides()
    {
        // Arrange
        (Booking booking, string email) = SeedBooking();
        (Booking other, _) = SeedBooking();
        string guestToken = await SignInAsync(booking, email);
        using HttpClient client = _factory.CreateStaffClient(StaffRole.Manager);
        client.DefaultRequestHeaders.Add(ApiAuthentication.GuestTokenHeader, guestToken);

        // Act
        using HttpResponseMessage otherBooking = await client.GetAsync($"api/bookings/{other.Id}", TestContext.Current.CancellationToken);
        using HttpResponseMessage guestRoute = await client.GetAsync(GuestBookingPath, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, otherBooking.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, guestRoute.StatusCode);
    }

    #endregion

    #region Helpers

    /// <summary>The text of a version 4 UUID in lower case (RFC 9562).</summary>
    private const string UuidOfVersion4 = "^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$";

    /// <summary>Every endpoint that requires a permission of staff, with a value for each route parameter.</summary>
    /// <returns>Pairs of HTTP method and address.</returns>
    public static TheoryData<string, string> StaffEndpoints()
    {
        TheoryData<string, string> endpoints = [];
        foreach (MethodInfo action in Controllers()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(action => action.GetCustomAttribute<RequiresPermissionAttribute>() is not null))
        {
            HttpMethodAttribute? verb = action.GetCustomAttributes<HttpMethodAttribute>().FirstOrDefault();
            if (verb is null)
            {
                continue;
            }

            string? prefix = action.DeclaringType?.GetCustomAttribute<RouteAttribute>()?.Template;
            string template = string.Join('/', new[] { prefix, verb.Template }.Where(part => !string.IsNullOrEmpty(part)));
            endpoints.Add(verb.HttpMethods.First(), string.Join('/', template.Split('/').Select(FillParameter)));
        }

        return endpoints;
    }

    private static IEnumerable<Type> Controllers() =>
        typeof(Program).Assembly.GetTypes().Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract);

    /// <summary>A value that meets the constraint of a route parameter; any other segment stays as it is.</summary>
    private static string FillParameter(string segment) =>
        !segment.StartsWith('{') ? segment
        : segment.Contains(":int", StringComparison.Ordinal) ? "1"
        : segment.Contains(":guid", StringComparison.Ordinal) ? "00000000-0000-0000-0000-000000000001"
        : "x";

    /// <summary>
    /// A network of its own for each test, from the range that IPv6 keeps for documentation (RFC 3849): the limit per
    /// address counts an IPv6 caller by its first 64 bits (ADR-0049 decision 6).
    /// </summary>
    private static string NextAddress() =>
        string.Create(CultureInfo.InvariantCulture, $"2001:db8:85:{Interlocked.Increment(ref s_nextAddress):x}::1");

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

    /// <summary>Signs in with the reference and the email, and gives the guest token; the sign-in must succeed.</summary>
    private async Task<string> SignInAsync(Booking booking, string email)
    {
        (HttpStatusCode status, string text) = await SignInRawAsync(BookingReference.Format(booking.Reference), email);
        Assert.True(status == HttpStatusCode.OK, $"The sign-in failed: {(int)status} {text}");
        return ValueOf<GuestSessionDto>(text).Token;
    }

    /// <summary>The limits of the guest sign-in of the API under test.</summary>
    private InMemoryGuestSignInLimits Limits() =>
        (InMemoryGuestSignInLimits)_factory.Services.GetRequiredService<IGuestSignInLimits>();

    /// <summary>Posts a sign-in from the address of this test, or from the address given.</summary>
    private async Task<(HttpStatusCode Status, string Text)> SignInRawAsync(string reference, string email, string? address = null)
    {
        using HttpClient client = _factory.CreateServiceClient();
        using HttpRequestMessage request = new(HttpMethod.Post, GuestLoginPath)
        {
            Content = JsonContent.Create(new { reference, email }, options: ApiJson.Options),
        };
        request.Headers.Add(ApiAuthentication.ClientAddressHeader, address ?? _address);
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        return (response.StatusCode, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    private async Task<(HttpStatusCode Status, string Text)> SignInCancelledAsync(Booking booking, string email)
    {
        booking.Cancel();
        return await SignInRawAsync(BookingReference.Format(booking.Reference), email);
    }

    /// <summary>Runs one guest operation with the token of the web application and, if given, a guest token.</summary>
    private Task<(HttpStatusCode Status, string Text)> RunAsync(string operation, Booking booking, string? guestToken) =>
        operation switch
        {
            ReadBooking => SendAsync(HttpMethod.Get, GuestBookingPath, guestToken),
            Cancel => SendAsync(HttpMethod.Post, GuestCancelPath, guestToken),
            Reschedule => SendAsync(
                HttpMethod.Post,
                GuestReschedulePath,
                guestToken,
                new { checkInDate = booking.StartDate.AddDays(10), checkOutDate = booking.EndDate.AddDays(10) }),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "Unknown operation."),
        };

    private async Task<(HttpStatusCode Status, string Text)> SendAsync(HttpMethod method, string path, string? guestToken, object? body = null)
    {
        using HttpClient client = _factory.CreateServiceClient();
        using HttpRequestMessage request = new(method, path);
        request.Headers.Add(ApiAuthentication.ClientAddressHeader, _address);
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

    /// <summary>The names of the properties of the value of an answer, in order.</summary>
    private static string[] PropertyNamesOf(string text)
    {
        using JsonDocument document = JsonDocument.Parse(text);
        return document.RootElement.GetProperty("value").EnumerateObject()
            .Select(property => property.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>The text of a property below the value of an answer.</summary>
    private static string ValueText(string text, params string[] path)
    {
        using JsonDocument document = JsonDocument.Parse(text);
        JsonElement element = document.RootElement.GetProperty("value");
        foreach (string name in path)
        {
            element = element.GetProperty(name);
        }

        return element.GetString() ?? string.Empty;
    }

    /// <summary>
    /// The text of an answer to a guest names neither the key of the booking nor its guest: no property for a key, no
    /// booking number of the old kind, no name, email or passport number.
    /// </summary>
    private void AssertNoKeyNoGuestAndNoPassport(string text, Booking booking)
    {
        Guest guest = _factory.World.Guests.Items.Single(item => item.Id == booking.GuestId);
        Assert.DoesNotContain("bookingId", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("guestId", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FLZ-", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(guest.Email, text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(guest.LastName, text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(PassportNumber, text, StringComparison.OrdinalIgnoreCase);
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
