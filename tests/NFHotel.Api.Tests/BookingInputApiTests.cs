using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Benchmark;
using NFHotel.Application.Errors;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Api.Tests;

/// <summary>
/// The rules of the booking input at the API, called without the page (MIL-086 criteria 3 to 6, ADR-0050): each field
/// of the guest at its limits and with each kind of value that its rule refuses, through the public booking and the
/// guest form of the back office; the dates with the fixed clock of the test world (today is 2026-01-01, so the five
/// years end on 2031-01-01); the party; an unknown field; the largest body; and the security event that a refusal
/// writes without the value.
/// </summary>
public sealed class BookingInputApiTests : IClassFixture<BookingInputApiTests.Factory>
{
    private const string PublicBookingPath = "api/bookings/public";
    private const string StaffBookingPath = "api/bookings";
    private const string StaffReschedulePath = "api/bookings/reschedule";
    private const string AvailableRoomsPath = "api/bookings/available-rooms";
    private const string OverviewPath = "api/bookings/overview";
    private const string GuestsPath = "api/guests";
    private const string SignInPath = "api/auth/guest-login";
    private const string BenchmarkResultsPath = "/api/benchmark/results";
    private const string SecurityCategory = "NFHotel.Security";
    private const string InputRefused = "input_refused";
    private const int BodyLimit = 65_536;

    // The stay that the move test books first: far from the stays of NextStay and from the last day of the five years.
    private const int MovedBookingOffset = 1500;

    private static readonly DateOnly Today = new(2026, 1, 1);
    private static readonly DateOnly LastDay = new(2031, 1, 1);
    private static int _daysUsed;
    private static int _address;

    private readonly Factory _factory;

    public BookingInputApiTests(Factory factory)
    {
        _factory = factory;
        _ = _factory.Server;
    }

    #region The fields of the guest

    /// <summary>The values of each field of the guest: at the limit, one above, and one of each refused kind, with the code that the API answers (none when accepted).</summary>
    public static TheoryData<string, string?, string?> GuestFieldCases() => new()
    {
        { "firstName", new string('a', 100), null },
        { "firstName", new string('a', 101), ErrorCodes.Guest.FirstNameTooLong },
        { "firstName", " ", ErrorCodes.Guest.FirstNameRequired },
        { "firstName", "Anna1", ErrorCodes.Guest.FirstNameInvalid },
        { "firstName", "<b>Anna</b>", ErrorCodes.Guest.FirstNameInvalid },
        { "lastName", new string('b', 100), null },
        { "lastName", new string('b', 101), ErrorCodes.Guest.LastNameTooLong },
        { "lastName", "", ErrorCodes.Guest.LastNameRequired },
        { "lastName", "Jensen_", ErrorCodes.Guest.LastNameInvalid },
        { "email", EmailOfLength(100), null },
        { "email", EmailOfLength(101), ErrorCodes.Guest.EmailTooLong },
        { "email", "", ErrorCodes.Guest.EmailRequired },
        { "email", "anna@example", ErrorCodes.Guest.EmailInvalid },
        { "email", "Anna <anna@example.com>", ErrorCodes.Guest.EmailInvalid },
        { "email", "anna@example.com\r\nBcc: eve@example.com", ErrorCodes.Guest.EmailInvalid },
        { "phoneNumber", "1234567", null },
        { "phoneNumber", "123456", ErrorCodes.Guest.PhoneNumberInvalid },
        { "phoneNumber", "+123456789012345", null },
        { "phoneNumber", "+1234567890123456", ErrorCodes.Guest.PhoneNumberInvalid },
        { "phoneNumber", PhoneNumberOfLength(50), null },
        { "phoneNumber", PhoneNumberOfLength(51), ErrorCodes.Guest.PhoneNumberTooLong },
        { "phoneNumber", "", ErrorCodes.Guest.PhoneNumberRequired },
        { "phoneNumber", "call me", ErrorCodes.Guest.PhoneNumberInvalid },
        { "country", "Denmark", null },
        { "country", "United Kingdom of Great Britain and Northern Ireland (the)", null },
        { "country", "Testland", ErrorCodes.Guest.CountryInvalid },
        { "country", "denmark", ErrorCodes.Guest.CountryInvalid },
        { "country", "", ErrorCodes.Guest.CountryRequired },
        { "passportNumber", new string('9', 20), null },
        { "passportNumber", new string('9', 21), ErrorCodes.Guest.PassportNumberTooLong },
        { "passportNumber", "AB-123", ErrorCodes.Guest.PassportNumberInvalid },
        { "passportNumber", null, null },
    };

    /// <summary>Criterion 3: the public booking accepts each field at its limit and refuses each other value with the code of the field alone.</summary>
    [Theory]
    [MemberData(nameof(GuestFieldCases))]
    public async Task PublicBooking_GuestField_IsAcceptedOrRefusedWithItsCode(string field, string? value, string? code)
    {
        // Arrange
        JsonObject body = PublicBody(NextStay(), RoomSize.Double, 1);
        body["newGuest"]![field] = value;

        // Act
        (HttpStatusCode status, string[]? codes) = await PostAsync(_factory.CreateServiceClient(), PublicBookingPath, body);

        // Assert
        AssertAnswer(code, status, codes);
    }

    /// <summary>Criterion 3: the guest form of the back office holds the same rules with the same codes.</summary>
    [Theory]
    [MemberData(nameof(GuestFieldCases))]
    public async Task GuestForm_GuestField_IsAcceptedOrRefusedWithItsCode(string field, string? value, string? code)
    {
        // Arrange
        JsonObject body = new() { ["fields"] = GuestBody() };
        body["fields"]![field] = value;

        // Act
        (HttpStatusCode status, string[]? codes) = await PostAsync(_factory.CreateAuthorizedClient(), GuestsPath, body);

        // Assert
        AssertAnswer(code, status, codes);
    }

    /// <summary>
    /// The hostile strings in each text field of the guest: the field, the hostile string, and the snake form of the field
    /// that each code of its refusal starts with.
    /// </summary>
    public static TheoryData<string, string> HostileFieldCases()
    {
        TheoryData<string, string> data = new();
        foreach (string field in new[] { "firstName", "lastName", "email", "phoneNumber", "country", "passportNumber" })
        {
            foreach ((string _, string? value) in HostileStrings.All)
            {
                data.Add(field, value);
            }
        }

        return data;
    }

    /// <summary>
    /// Criterion 5: each hostile string in each text field is either refused with the codes of that field alone or stored
    /// unchanged, as the guest record shows it.
    /// </summary>
    [Theory]
    [MemberData(nameof(HostileFieldCases))]
    public async Task GuestForm_HostileString_IsRefusedByTheFieldOrStoredUnchanged(string field, string value)
    {
        // Arrange
        using HttpClient client = _factory.CreateAuthorizedClient();
        JsonObject body = new() { ["fields"] = GuestBody() };
        body["fields"]![field] = value;

        // Act
        using HttpResponseMessage response = await client.PostAsync(GuestsPath, Json(body), TestContext.Current.CancellationToken);
        string text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            int id = JsonNode.Parse(text)!["value"]!["guestId"]!.GetValue<int>();
            Guest stored = _factory.World.Guests.Items.Single(guest => guest.Id == id);
            Assert.Equal(value, FieldOf(stored, field));
        }
        else
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            string prefix = $"guest.{Snake(field)}_";
            Assert.All(Codes(text), code => Assert.StartsWith(prefix, code, StringComparison.Ordinal));
        }
    }

    /// <summary>Criterion 5: the two searches of the back office take each hostile string as text, and none is a fault.</summary>
    [Fact]
    public async Task Searches_HostileStrings_AreTextAndNoFault()
    {
        // Arrange
        using HttpClient client = _factory.CreateAuthorizedClient();

        foreach ((string? kind, string? value) in HostileStrings.All)
        {
            // Act
            using HttpResponseMessage guests = await client.GetAsync($"{GuestsPath}?searchText={Uri.EscapeDataString(value)}", TestContext.Current.CancellationToken);
            JsonObject overview = new()
            {
                ["periodStart"] = Day(Today),
                ["periodEnd"] = Day(Today.AddDays(30)),
                ["searchText"] = value,
            };
            (HttpStatusCode overviewStatus, string[] _) = await PostOnSharedClientAsync(client, OverviewPath, overview);

            // Assert
            Assert.True(guests.StatusCode == HttpStatusCode.OK, $"The guest search answered {guests.StatusCode} to {kind}.");
            Assert.True(overviewStatus == HttpStatusCode.OK, $"The overview answered {overviewStatus} to {kind}.");
        }
    }

    #endregion

    #region The stay and the party

    /// <summary>Criterion 4: on the public site 30 nights are booked and 31 refused; staff book 31.</summary>
    [Fact]
    public async Task Nights_PublicThirtyAndStaffThirtyOne_AreBookedAndPublicThirtyOneIsRefused()
    {
        // Arrange
        (DateOnly CheckIn, DateOnly CheckOut) thirty = NextStay(30);
        (DateOnly CheckIn, DateOnly CheckOut) thirtyOne = NextStay(31);
        (DateOnly CheckIn, DateOnly CheckOut) staffThirtyOne = NextStay(31);

        // Act
        (HttpStatusCode publicThirty, string[] _) = await PostAsync(_factory.CreateServiceClient(), PublicBookingPath, PublicBody(thirty, RoomSize.Double, 1));
        (HttpStatusCode publicThirtyOne, string[]? codes) = await PostAsync(_factory.CreateServiceClient(), PublicBookingPath, PublicBody(thirtyOne, RoomSize.Double, 1));
        (HttpStatusCode staff, string[] _) = await PostAsync(_factory.CreateAuthorizedClient(), StaffBookingPath, StaffBody(staffThirtyOne, FreeRoom(RoomSize.Single)));

        // Assert
        Assert.Equal(HttpStatusCode.OK, publicThirty);
        Assert.Equal((HttpStatusCode.UnprocessableEntity, ErrorCodes.Booking.StayTooLong), (publicThirtyOne, string.Join(", ", codes)));
        Assert.Equal(HttpStatusCode.OK, staff);
    }

    /// <summary>Criterion 4: a stay that ends exactly five years after today is booked; one that ends a day later is refused, for a guest and for staff.</summary>
    [Fact]
    public async Task Departure_FiveYearsAheadAndADayLater_IsBookedOrRefused()
    {
        // Arrange
        (DateOnly, DateOnly LastDay) last = (LastDay.AddDays(-2), LastDay);
        (DateOnly, DateOnly) later = (LastDay.AddDays(-1), LastDay.AddDays(1));

        // Act
        (HttpStatusCode publicLast, string[] _) = await PostAsync(_factory.CreateServiceClient(), PublicBookingPath, PublicBody(last, RoomSize.Suite, 1));
        (HttpStatusCode publicLater, string[]? publicCodes) = await PostAsync(_factory.CreateServiceClient(), PublicBookingPath, PublicBody(later, RoomSize.Suite, 1));
        (HttpStatusCode staffLater, string[]? staffCodes) = await PostAsync(_factory.CreateAuthorizedClient(), StaffBookingPath, StaffBody(later, FreeRoom(RoomSize.Single)));

        // Assert
        Assert.Equal(HttpStatusCode.OK, publicLast);
        Assert.Equal((HttpStatusCode.UnprocessableEntity, ErrorCodes.Booking.EndDateTooFarAhead), (publicLater, string.Join(", ", publicCodes)));
        Assert.Equal((HttpStatusCode.UnprocessableEntity, ErrorCodes.Booking.EndDateTooFarAhead), (staffLater, string.Join(", ", staffCodes)));
    }

    /// <summary>Criterion 4: staff cannot move a booking to end beyond the five years, and the free rooms are not looked up there either.</summary>
    [Fact]
    public async Task MoveAndFreeRooms_BeyondTheFiveYears_AreRefused()
    {
        // Arrange
        Room room = FreeRoom(RoomSize.Double);
        Booking booking = _factory.World.AddBooking(room.Id, _factory.World.AddGuest().Id, startOffsetDays: MovedBookingOffset, nights: 1);
        JsonObject move = new()
        {
            ["bookingId"] = booking.Id,
            ["newStartDate"] = Day(LastDay.AddDays(-1)),
            ["newEndDate"] = Day(LastDay.AddDays(1)),
            ["roomId"] = room.Id,
        };
        using HttpClient client = _factory.CreateAuthorizedClient();

        // Act
        (HttpStatusCode moved, string[]? moveCodes) = await PostOnSharedClientAsync(client, StaffReschedulePath, move);
        using HttpResponseMessage free = await client.GetAsync($"{AvailableRoomsPath}?checkInDate={Day(LastDay)}&checkOutDate={Day(LastDay.AddDays(1))}", TestContext.Current.CancellationToken);
        using HttpResponseMessage past = await client.GetAsync($"{AvailableRoomsPath}?checkInDate={Day(Today.AddDays(-1))}&checkOutDate={Day(Today.AddDays(1))}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((HttpStatusCode.UnprocessableEntity, ErrorCodes.Booking.EndDateTooFarAhead), (moved, string.Join(", ", moveCodes)));
        Assert.Equal((HttpStatusCode.UnprocessableEntity, ErrorCodes.Booking.EndDateTooFarAhead), (free.StatusCode, string.Join(", ", Codes(await free.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)))));
        Assert.Equal((HttpStatusCode.UnprocessableEntity, ErrorCodes.Booking.StartDateInPast), (past.StatusCode, string.Join(", ", Codes(await past.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)))));
    }

    /// <summary>Criterion 4: an arrival yesterday is refused on the public site.</summary>
    [Fact]
    public async Task PublicBooking_ArrivalYesterday_IsRefused()
    {
        // Act
        (HttpStatusCode status, string[]? codes) = await PostAsync(_factory.CreateServiceClient(), PublicBookingPath, PublicBody((Today.AddDays(-1), Today.AddDays(1)), RoomSize.Double, 1));

        // Assert
        Assert.Equal((HttpStatusCode.UnprocessableEntity, ErrorCodes.Booking.StartDateInPast), (status, string.Join(", ", codes)));
    }

    /// <summary>Criterion 3: a party of 1 and 4 is booked; 0 and 5 are refused with the code of the field.</summary>
    [Theory]
    [InlineData(1, null)]
    [InlineData(4, null)]
    [InlineData(0, ErrorCodes.Booking.GuestsOutOfRange)]
    [InlineData(5, ErrorCodes.Booking.GuestsOutOfRange)]
    public async Task PublicBooking_Party_IsOneToFour(int guests, string? code)
    {
        // Act
        (HttpStatusCode status, string[]? codes) = await PostAsync(_factory.CreateServiceClient(), PublicBookingPath, PublicBody(NextStay(), RoomSize.Suite, guests));

        // Assert
        AssertAnswer(code, status, codes);
    }

    /// <summary>Criterion 3: a public booking without a room size, or with one that does not exist, is refused by the binding.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData(3)]
    public async Task PublicBooking_RoomSizeMissingOrUnknown_IsRefused(int? size)
    {
        // Arrange
        JsonObject body = PublicBody(NextStay(), RoomSize.Double, 1);
        body.Remove("roomSize");
        if (size is not null)
        {
            body["roomSize"] = size;
        }

        // Act
        (HttpStatusCode status, string[]? codes) = await PostAsync(_factory.CreateServiceClient(), PublicBookingPath, body);

        // Assert
        Assert.Equal((HttpStatusCode.BadRequest, ApiErrorCodes.InvalidRequest), (status, string.Join(", ", codes)));
    }

    #endregion

    #region The shape and the size of a request

    /// <summary>Criterion 3: a field that the request type does not have is refused, at the top and inside the guest.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicBooking_UnknownField_IsRefused(bool insideTheGuest)
    {
        // Arrange
        JsonObject body = PublicBody(NextStay(), RoomSize.Double, 1);
        (insideTheGuest ? body["newGuest"]!.AsObject() : body)["isAdmin"] = true;

        // Act
        (HttpStatusCode status, string[]? codes) = await PostAsync(_factory.CreateServiceClient(), PublicBookingPath, body);

        // Assert
        Assert.Equal((HttpStatusCode.BadRequest, ApiErrorCodes.InvalidRequest), (status, string.Join(", ", codes)));
        Assert.DoesNotContain(_factory.World.Guests.Items, guest => guest.Email == "unknown-field@example.com");
    }

    /// <summary>Criterion 3: a body of 64 kilobytes is read, and a body one byte larger gets 413 with its code.</summary>
    [Fact]
    public async Task PublicBooking_BodyAtAndOverTheLimit_IsReadOrRefusedWith413()
    {
        // Arrange
        StringContent atTheLimit = Padded(PublicBody(NextStay(), RoomSize.Double, 1), BodyLimit);
        StringContent overTheLimit = Padded(PublicBody(NextStay(), RoomSize.Double, 1), BodyLimit + 1);
        using HttpClient client = _factory.CreateServiceClient();

        // Act
        using HttpResponseMessage read = await client.PostAsync(PublicBookingPath, atTheLimit, TestContext.Current.CancellationToken);
        using HttpResponseMessage refused = await client.PostAsync(PublicBookingPath, overTheLimit, TestContext.Current.CancellationToken);
        string refusedText = await refused.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, refused.StatusCode);
        Assert.Equal(new[] { ApiErrorCodes.RequestTooLarge }, Codes(refusedText));
    }

    /// <summary>
    /// MIL-086 decision 4 at the server that production runs: a body sent in chunks without a declared length, as the
    /// web application sends its requests, is refused with 413 once it grows over 64 KB, and the refusal writes the event.
    /// </summary>
    [Fact]
    public async Task PublicBooking_ChunkedBodyOverTheLimit_IsRefusedWith413ByTheServer()
    {
        // Arrange
        using Factory server = new();
        server.UseKestrel(0);
        using HttpClient client = server.CreateServiceClient();
        using HttpRequestMessage request = new(HttpMethod.Post, PublicBookingPath)
        {
            Content = Padded(PublicBody(NextStay(), RoomSize.Double, 1), BodyLimit + 1024),
        };
        request.Headers.TransferEncodingChunked = true;

        // Act
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        string text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Equal(new[] { ApiErrorCodes.RequestTooLarge }, Codes(text));
        RecordingLoggerProvider.LogEntry entry = Assert.Single(
            server.Logs.Entries,
            logged => logged.Category == SecurityCategory && logged.AllText.Contains(InputRefused, StringComparison.Ordinal));
        Assert.Contains("413", entry.AllText, StringComparison.Ordinal);
        Assert.Contains(PublicBookingPath, entry.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// ADR-0050 at the server that production runs: the intake of a benchmark run keeps its larger limit, so a run with a
    /// long model output, sent in chunks without a declared length, is read whole and stored.
    /// </summary>
    [Fact]
    public async Task BenchmarkIntake_ChunkedRunOfMoreThan64Kilobytes_IsStoredByTheServer()
    {
        // Arrange
        using Factory server = new();
        server.UseKestrel(0);
        using HttpClient client = server.CreateServiceClient();
        DateTimeOffset start = new(2026, 10, 19, 10, 0, 0, TimeSpan.Zero);
        BenchmarkRequestResultDto result = new(2, start, "s1", "ollama", "m1", "hotel-cancellation", "analysis", "hotel_cancellation", 0, true, 0.5, 2.0, 10, 20, 5.0, false, "stop", null, null, 1000, null, null, null, null, true, null, $"{{\"text\":\"{new string('x', 100_000)}\"}}", null);
        SubmitBenchmarkRunRequest run = new(
            2,
            Guid.Parse("5b3f0e1a-0000-4000-8000-000000000086"),
            start,
            start.AddMinutes(5),
            "hotel-v1",
            new BenchmarkDatasetDto("d.csv", new string('a', 64), 10),
            new BenchmarkMachineDto("Test CPU", 8, 16384, null, "Linux 6.8"),
            new BenchmarkBackendDto("ollama", "0.5.1"),
            [result]);
        using HttpRequestMessage request = new(HttpMethod.Post, BenchmarkResultsPath) { Content = JsonContent.Create(run) };
        request.Headers.TransferEncodingChunked = true;

        // Act
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single(server.World.Benchmarks.Items);
    }

    /// <summary>
    /// MIL-086 decision 4: a text without a length of its own has at most 1,000 characters. The reference of the guest
    /// sign-in has none, so 1,000 characters are refused as a failed sign-in and 1,001 by the input rules.
    /// </summary>
    [Theory]
    [InlineData(1000, HttpStatusCode.Unauthorized, ErrorCodes.Api.GuestUnauthorized)]
    [InlineData(1001, HttpStatusCode.BadRequest, ApiErrorCodes.InvalidRequest)]
    public async Task SignIn_ReferenceOfAThousandCharacters_IsTheLongestThatIsRead(int length, HttpStatusCode expected, string code)
    {
        // Arrange
        using HttpClient client = _factory.CreateServiceClient();
        using HttpRequestMessage request = new(HttpMethod.Post, SignInPath)
        {
            Content = Json(new JsonObject { ["reference"] = new string('a', length), ["email"] = "anna@example.com" }),
        };
        request.Headers.Add(ApiAuthentication.ClientAddressHeader, NextAddress());

        // Act
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        string text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((expected, code), (response.StatusCode, string.Join(", ", Codes(text))));
    }

    #endregion

    #region The security event

    /// <summary>
    /// Criterion 6, ASVS 16.3.3: a refused value writes one security event with the method, the route of the endpoint,
    /// the status and the codes, and never the value.
    /// </summary>
    [Fact]
    public async Task Refusal_OfAField_WritesOneSecurityEventWithoutTheValue()
    {
        // Arrange
        const string Value = "<script>alert('mil-086')</script>";
        JsonObject body = PublicBody(NextStay(), RoomSize.Double, 1);
        body["newGuest"]!["firstName"] = Value;
        _factory.Logs.Clear();

        // Act
        (HttpStatusCode status, string[] _) = await PostAsync(_factory.CreateServiceClient(), PublicBookingPath, body);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, status);
        RecordingLoggerProvider.LogEntry entry = Assert.Single(SecurityEvents());
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Contains("POST", entry.AllText, StringComparison.Ordinal);
        Assert.Contains("api/bookings/public", entry.AllText, StringComparison.Ordinal);
        Assert.Contains("422", entry.AllText, StringComparison.Ordinal);
        Assert.Contains(ErrorCodes.Guest.FirstNameInvalid, entry.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("mil-086", entry.AllText, StringComparison.Ordinal);
    }

    /// <summary>Criterion 6: the refusals of the binding, the input rules and the body limit write the event too, without the query or the body.</summary>
    [Fact]
    public async Task Refusal_BeforeTheAction_WritesTheSecurityEventWithoutTheRequest()
    {
        // Arrange
        JsonObject unknown = PublicBody(NextStay(), RoomSize.Double, 1);
        unknown["secret-field-mil-086"] = 1;
        StringContent tooLarge = Padded(PublicBody(NextStay(), RoomSize.Double, 1), BodyLimit + 1);
        string longSearch = new string('z', 200) + "secret-query-mil-086";
        using HttpClient service = _factory.CreateServiceClient();
        using HttpClient staff = _factory.CreateAuthorizedClient();
        _factory.Logs.Clear();

        // Act
        (HttpStatusCode unknownStatus, string[] _) = await PostOnSharedClientAsync(service, PublicBookingPath, unknown);
        using HttpResponseMessage large = await service.PostAsync(PublicBookingPath, tooLarge, TestContext.Current.CancellationToken);
        using HttpResponseMessage search = await staff.GetAsync($"{GuestsPath}?searchText={longSearch}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            (HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge, HttpStatusCode.BadRequest),
            (unknownStatus, large.StatusCode, search.StatusCode));
        List<RecordingLoggerProvider.LogEntry> events = SecurityEvents();
        Assert.Equal(3, events.Count);
        Assert.Contains(events, entry => entry.AllText.Contains(ApiErrorCodes.InvalidRequest, StringComparison.Ordinal) && entry.AllText.Contains("api/bookings/public", StringComparison.Ordinal));
        Assert.Contains(events, entry => entry.AllText.Contains(ApiErrorCodes.RequestTooLarge, StringComparison.Ordinal) && entry.AllText.Contains("413", StringComparison.Ordinal));
        Assert.Contains(events, entry => entry.AllText.Contains("GET", StringComparison.Ordinal) && entry.AllText.Contains("api/guests", StringComparison.Ordinal));
        Assert.All(events, entry => Assert.DoesNotContain("mil-086", entry.AllText, StringComparison.Ordinal));
    }

    /// <summary>
    /// ADR-0050 decision 6: the 400 of the handler of exceptions writes the event too, so a key below one, which the
    /// service refuses as an error of the caller, is counted like the other refusals.
    /// </summary>
    [Fact]
    public async Task Refusal_ByTheHandlerOfExceptions_WritesTheSecurityEvent()
    {
        // Arrange
        Room room = FreeRoom(RoomSize.Double);
        (DateOnly checkIn, DateOnly checkOut) = NextStay();
        JsonObject move = new()
        {
            ["bookingId"] = 0,
            ["newStartDate"] = Day(checkIn),
            ["newEndDate"] = Day(checkOut),
            ["roomId"] = room.Id,
        };
        using HttpClient client = _factory.CreateAuthorizedClient();
        _factory.Logs.Clear();

        // Act
        (HttpStatusCode status, string[] codes) = await PostOnSharedClientAsync(client, StaffReschedulePath, move);

        // Assert
        Assert.Equal((HttpStatusCode.BadRequest, ApiErrorCodes.InvalidRequest), (status, string.Join(",", codes)));
        RecordingLoggerProvider.LogEntry entry = Assert.Single(SecurityEvents());
        Assert.Contains("400", entry.AllText, StringComparison.Ordinal);
        Assert.Contains(StaffReschedulePath, entry.AllText, StringComparison.Ordinal);
    }

    #endregion

    #region Helpers

    private List<RecordingLoggerProvider.LogEntry> SecurityEvents() =>
        _factory.Logs.Entries
            .Where(entry => entry.Category == SecurityCategory && entry.AllText.Contains(InputRefused, StringComparison.Ordinal))
            .ToList();

    private static void AssertAnswer(string? code, HttpStatusCode status, string[] codes)
    {
        if (code is null)
        {
            Assert.True(status == HttpStatusCode.OK, $"Refused with {status}: {string.Join(", ", codes)}");
        }
        else
        {
            Assert.Equal((HttpStatusCode.UnprocessableEntity, code), (status, string.Join(", ", codes)));
        }
    }

    // Posts with a client made for this one call and disposes it.
    private static async Task<(HttpStatusCode Status, string[] Codes)> PostAsync(HttpClient client, string path, JsonObject body)
    {
        using (client)
        {
            return await PostOnSharedClientAsync(client, path, body);
        }
    }

    // Posts with a client that the test goes on using; the test disposes it.
    private static async Task<(HttpStatusCode Status, string[] Codes)> PostOnSharedClientAsync(HttpClient client, string path, JsonObject body)
    {
        using HttpResponseMessage response = await client.PostAsync(path, Json(body), TestContext.Current.CancellationToken);
        return (response.StatusCode, Codes(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)));
    }

    private static string[] Codes(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        JsonArray? errors = JsonNode.Parse(text)?["errors"]?.AsArray();
        return errors is null ? [] : errors.Select(error => error!.GetValue<string>()).ToArray();
    }

    private static StringContent Json(JsonObject body) => new(body.ToJsonString(), Encoding.UTF8, "application/json");

    // A body of an exact size: the JSON with spaces after it, which JSON allows.
    private static StringContent Padded(JsonObject body, int size)
    {
        string json = body.ToJsonString();
        return new StringContent(json + new string(' ', size - Encoding.UTF8.GetByteCount(json)), Encoding.UTF8, "application/json");
    }

    private static JsonObject PublicBody((DateOnly CheckIn, DateOnly CheckOut) stay, RoomSize size, int guests) => new()
    {
        ["checkInDate"] = Day(stay.CheckIn),
        ["checkOutDate"] = Day(stay.CheckOut),
        ["roomSize"] = (int)size,
        ["guests"] = guests,
        ["newGuest"] = GuestBody(),
    };

    private static JsonObject StaffBody((DateOnly CheckIn, DateOnly CheckOut) stay, Room room) => new()
    {
        ["checkInDate"] = Day(stay.CheckIn),
        ["checkOutDate"] = Day(stay.CheckOut),
        ["roomId"] = room.Id,
        ["guestId"] = null,
        ["newGuest"] = GuestBody(),
    };

    private static JsonObject GuestBody() => new()
    {
        ["firstName"] = "Anna",
        ["lastName"] = "Jensen",
        ["email"] = "anna@example.com",
        ["phoneNumber"] = "+4512345678",
        ["country"] = "Denmark",
        ["passportNumber"] = null,
    };

    // A stay of its own for each request, right after the stay before it, so that no two requests of this class want the
    // same room on the same night and every stay stays well inside the five years.
    private static (DateOnly CheckIn, DateOnly CheckOut) NextStay(int nights = 2)
    {
        int end = Interlocked.Add(ref _daysUsed, nights + 1);
        DateOnly start = Today.AddDays(10 + end - nights - 1);
        return (start, start.AddDays(nights));
    }

    private Room FreeRoom(RoomSize size) => _factory.World.Rooms.Items.First(room => room.Size == size);

    private static string Day(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string NextAddress() => $"203.0.113.{(Interlocked.Increment(ref _address) % 250) + 1}";

    private static string EmailOfLength(int length)
    {
        const string Domain = "example.com";
        string local = new('a', 64);
        string label = new('b', length - local.Length - Domain.Length - 2);
        return $"{local}@{label}.{Domain}";
    }

    private static string PhoneNumberOfLength(int length) => $"12{new string(' ', length - 8)}345678";

    private static string Snake(string field) => field switch
    {
        "firstName" => "first_name",
        "lastName" => "last_name",
        "phoneNumber" => "phone_number",
        "passportNumber" => "passport_number",
        _ => field,
    };

    private static string? FieldOf(Guest guest, string field) => field switch
    {
        "firstName" => guest.FirstName,
        "lastName" => guest.LastName,
        "email" => guest.Email,
        "phoneNumber" => guest.PhoneNumber,
        "country" => guest.Country,
        "passportNumber" => guest.PassportNumber,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Not a field of the guest."),
    };

    #endregion

    /// <summary>The API with ten rooms of each size that sleep four, and a record of every log entry.</summary>
    public sealed class Factory : ApiFactory
    {
        public Factory()
        {
            for (int number = 0; number < 30; number++)
            {
                World.AddRoom((101 + number).ToString(CultureInfo.InvariantCulture), 1, (RoomSize)(number % 3), 4);
            }
        }

        /// <summary>The log entries that the API wrote.</summary>
        public RecordingLoggerProvider Logs { get; } = new();

        /// <inheritdoc />
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        }
    }
}
