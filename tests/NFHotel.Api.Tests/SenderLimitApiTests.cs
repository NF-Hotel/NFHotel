using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.External.Telemetry;
using NFHotel.Infrastructure.Config;
using NFHotel.Infrastructure.Security;
using NFHotel.TestSupport;

namespace NFHotel.Api.Tests;

/// <summary>
/// The limits on what one caller can ask of the booking flow, through the API (MIL-087 criteria 3 to 6, ADR-0051): each
/// limit per caller address at its number with the fixed clock of the test world, the limit per email, the address that a
/// caller cannot name, and the security events and their counter. Each test starts a host of its own, so no count of one
/// test reaches another.
/// </summary>
public sealed class SenderLimitApiTests
{
    private const string AvailableRoomsPath = "api/bookings/available-rooms";
    private const string PublicBookingPath = "api/bookings/public";
    private const string StaffBookingPath = "api/bookings";
    private const string GuestSignInPath = "api/auth/guest-login";
    private const string StaffSignInPath = "/api/auth/login";
    private const string SecurityCategory = "NFHotel.Security";
    private const string Password = "correct horse battery staple";

    /// <summary>The header in which a test names the peer of the connection, which the in-memory server does not have.</summary>
    private const string PeerHeader = "X-Test-Peer";

    /// <summary>The address of the caller that most tests send from.</summary>
    private const string Address = "203.0.113.10";

    private static readonly DateOnly FirstStay = new(2026, 3, 1);

    #region The search for free rooms

    /// <summary>
    /// Boundary (criterion 3): the 60th search of one address in a minute is answered; the 61st, a second before the
    /// minute ends, gets 429 with the code and a <c>Retry-After</c> of the one second that is left.
    /// </summary>
    [Fact]
    public async Task Search_SixtyFirstInAMinute_IsRefusedWithTheCodeAndTheTimeToWait()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        HttpStatusCode[] allowed = await SearchManyAsync(client, Address, 60);
        factory.World.Clock.TimeOfDay += TimeSpan.FromSeconds(59);

        // Act
        using HttpResponseMessage refused = await SearchAsync(client, Address);

        // Assert
        Assert.All(allowed, status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal([ErrorCodes.Booking.TooManyRequests], await CodesAsync(refused));
        Assert.Equal(TimeSpan.FromSeconds(1), refused.Headers.RetryAfter?.Delta);
    }

    /// <summary>
    /// Boundary (review RC-428 F12): the time to wait is rounded up to whole seconds, never down or to the nearest:
    /// half a second left is one second, 1.8 seconds are two, and 1.3 seconds are two.
    /// </summary>
    /// <param name="elapsedMilliseconds">The time between the first search and the refused one.</param>
    /// <param name="wait">The whole seconds of <c>Retry-After</c>.</param>
    [Theory]
    [InlineData(59_500, 1)]
    [InlineData(58_200, 2)]
    [InlineData(58_700, 2)]
    public async Task Search_RefusedWithPartOfASecondLeft_NamesTheWholeSecondsRoundedUp(int elapsedMilliseconds, int wait)
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        await SearchManyAsync(client, Address, 60);
        factory.World.Clock.TimeOfDay += TimeSpan.FromMilliseconds(elapsedMilliseconds);

        // Act
        using HttpResponseMessage refused = await SearchAsync(client, Address);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(wait), refused.Headers.RetryAfter?.Delta);
    }

    /// <summary>Boundary (criterion 3): when the minute of the window has passed, the count starts again.</summary>
    [Fact]
    public async Task Search_AfterTheMinute_IsAnsweredAgain()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        await SearchManyAsync(client, Address, 60);
        using HttpResponseMessage refused = await SearchAsync(client, Address);
        factory.World.Clock.TimeOfDay += TimeSpan.FromMinutes(1);

        // Act
        using HttpResponseMessage answered = await SearchAsync(client, Address);

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, answered.StatusCode);
    }

    /// <summary>
    /// Security (criterion 3, ADR-0051 decision 2): searches sent at the same time cannot all pass the check before any of
    /// them is counted; of 100 from one address, 60 are answered.
    /// </summary>
    [Fact]
    public async Task Search_HundredAtTheSameTime_AnswersSixty()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();

        // Act
        HttpStatusCode[] statuses = await Task.WhenAll(Enumerable.Range(0, 100).Select(async _ =>
        {
            using HttpResponseMessage response = await SearchAsync(client, Address);
            return response.StatusCode;
        }));

        // Assert
        Assert.Equal(60, statuses.Count(status => status == HttpStatusCode.OK));
        Assert.Equal(40, statuses.Count(status => status == HttpStatusCode.TooManyRequests));
    }

    /// <summary>Equivalence (criterion 5): the searches of one address do not use up the count of another.</summary>
    [Fact]
    public async Task Search_OfAnotherAddress_IsNotStopped()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        await SearchManyAsync(client, Address, 60);
        using HttpResponseMessage refused = await SearchAsync(client, Address);

        // Act
        using HttpResponseMessage other = await SearchAsync(client, "203.0.113.11");

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
    }

    #endregion

    #region The booking requests

    /// <summary>
    /// Boundary (criterion 3): the fifth booking request of one address in an hour is made; the sixth, ten minutes into
    /// the hour, gets 429 with the code and a <c>Retry-After</c> of the fifty minutes that are left.
    /// </summary>
    [Fact]
    public async Task Booking_SixthInAnHour_IsRefusedWithTheCodeAndTheTimeToWait()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        HttpStatusCode[] made = await BookManyAsync(client, Address, 0, 5);
        factory.World.Clock.TimeOfDay += TimeSpan.FromMinutes(10);

        // Act
        using HttpResponseMessage refused = await BookAsync(client, Address, 5, Email(5));

        // Assert
        Assert.All(made, status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal([ErrorCodes.Booking.TooManyRequests], await CodesAsync(refused));
        Assert.Equal(TimeSpan.FromMinutes(50), refused.Headers.RetryAfter?.Delta);
        Assert.Equal(5, factory.World.Bookings.Items.Count);
    }

    /// <summary>Boundary (criterion 3): when the hour of the window has passed, the count of the hour starts again.</summary>
    [Fact]
    public async Task Booking_AfterTheHour_IsMadeAgain()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        await BookManyAsync(client, Address, 0, 5);
        using HttpResponseMessage refused = await BookAsync(client, Address, 5, Email(5));
        factory.World.Clock.TimeOfDay += TimeSpan.FromHours(1);

        // Act
        using HttpResponseMessage made = await BookAsync(client, Address, 6, Email(6));

        // Assert
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal(HttpStatusCode.OK, made.StatusCode);
    }

    /// <summary>
    /// Boundary (criterion 3): five requests in each of four hours make twenty; the 21st, in the fifth hour, is refused by
    /// the limit of the day with the twenty hours that are left of it, and the next day the count starts again.
    /// </summary>
    [Fact]
    public async Task Booking_TwentyFirstInADay_IsRefusedByTheLimitOfTheDay()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        List<HttpStatusCode> made = [];
        for (int hour = 0; hour < 4; hour++)
        {
            made.AddRange(await BookManyAsync(client, Address, hour * 5, 5));
            factory.World.Clock.TimeOfDay += TimeSpan.FromHours(1);
        }

        // Act
        using HttpResponseMessage refused = await BookAsync(client, Address, 20, Email(20));
        factory.World.Clock.Today = factory.World.Clock.Today.AddDays(1);
        factory.World.Clock.TimeOfDay = TimeSpan.FromHours(12);
        using HttpResponseMessage nextDay = await BookAsync(client, Address, 21, Email(21));

        // Assert
        Assert.Equal(20, made.Count(status => status == HttpStatusCode.OK));
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal(TimeSpan.FromHours(20), refused.Headers.RetryAfter?.Delta);
        Assert.Contains(factory.Logs.Entries, entry => entry.Category == SecurityCategory
            && entry.Message.StartsWith(SecurityEvents.LimitRefusedKind, StringComparison.Ordinal)
            && entry.Message.Contains(LimitNames.BookingDay, StringComparison.Ordinal));
        Assert.Equal(HttpStatusCode.OK, nextDay.StatusCode);
    }

    /// <summary>
    /// Equivalence (criterion 3, ADR-0051 decision 2): a request that a rule of the use case refuses with 422 gets its
    /// count back, so six of them use up nothing and five bookings are still made.
    /// </summary>
    [Fact]
    public async Task Booking_RefusedByARuleOfTheUseCase_GetsItsCountBack()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        List<HttpStatusCode> refusedByTheRule = [];
        for (int request = 0; request < 6; request++)
        {
            JsonObject body = PublicBody(request, Email(request));
            body["checkInDate"] = Day(factory.World.Clock.Today.AddDays(-1));
            body["checkOutDate"] = Day(factory.World.Clock.Today.AddDays(1));
            using HttpResponseMessage response = await PostAsync(client, PublicBookingPath, body, Address);
            refusedByTheRule.Add(response.StatusCode);
        }

        // Act
        HttpStatusCode[] made = await BookManyAsync(client, Address, 10, 5);
        using HttpResponseMessage refused = await BookAsync(client, Address, 15, Email(15));

        // Assert
        Assert.All(refusedByTheRule, status => Assert.Equal(HttpStatusCode.UnprocessableEntity, status));
        Assert.All(made, status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
    }

    /// <summary>
    /// Negative (criterion 3, MIL-086 dependency): a request that the rules of the fields refuse is refused before the
    /// count, with 422 for a field and 400 for a field that the request type does not have, so it uses up nothing.
    /// </summary>
    [Fact]
    public async Task Booking_RefusedByTheRulesOfTheFields_IsNotCounted()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        List<HttpStatusCode> refusedByTheFields = [];
        for (int request = 0; request < 3; request++)
        {
            using HttpResponseMessage response = await BookAsync(client, Address, request, "anna@example");
            refusedByTheFields.Add(response.StatusCode);
        }

        for (int request = 3; request < 6; request++)
        {
            JsonObject body = PublicBody(request, Email(request));
            body["isAdmin"] = true;
            using HttpResponseMessage response = await PostAsync(client, PublicBookingPath, body, Address);
            refusedByTheFields.Add(response.StatusCode);
        }

        // Act
        HttpStatusCode[] made = await BookManyAsync(client, Address, 10, 5);
        using HttpResponseMessage refused = await BookAsync(client, Address, 15, Email(15));

        // Assert
        Assert.Equal(
            [HttpStatusCode.UnprocessableEntity, HttpStatusCode.UnprocessableEntity, HttpStatusCode.UnprocessableEntity,
                HttpStatusCode.BadRequest, HttpStatusCode.BadRequest, HttpStatusCode.BadRequest],
            refusedByTheFields);
        Assert.All(made, status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
    }

    #endregion

    #region The email

    /// <summary>
    /// Boundary (criterion 3, ADR-0051 decision 3): three coming bookings of one email are made from three addresses; the
    /// fourth, from a fourth address and with the email in other letter case, gets 429 with the code and without
    /// <c>Retry-After</c>, because the limit has no window, and stores nothing.
    /// </summary>
    [Fact]
    public async Task Booking_FourthComingBookingOfOneEmail_IsRefusedWithoutTheTimeToWait()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        List<HttpStatusCode> made = [];
        for (int booking = 0; booking < 3; booking++)
        {
            using HttpResponseMessage response = await BookAsync(client, $"203.0.113.{20 + booking}", booking, "anna@example.com");
            made.Add(response.StatusCode);
        }

        int guestsBefore = factory.World.Guests.Items.Count;

        // Act
        using HttpResponseMessage refused = await BookAsync(client, "203.0.113.23", 3, "Anna@Example.com");

        // Assert
        Assert.All(made, status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal([ErrorCodes.Booking.TooManyRequests], await CodesAsync(refused));
        Assert.Null(refused.Headers.RetryAfter);
        Assert.Equal(guestsBefore, factory.World.Guests.Items.Count);
        Assert.Equal(3, factory.World.Bookings.Items.Count);
    }

    /// <summary>
    /// Equivalence (criterion 3, ADR-0051 decision 2): a request that the limit per email refuses keeps its count of the
    /// address, because a limit refused it, not a rule. After three bookings of one email and the refused fourth from
    /// one address, the fifth request, of another email, is made and the sixth is refused by the limit of the hour.
    /// </summary>
    [Fact]
    public async Task Booking_RefusedByTheLimitPerEmail_KeepsItsCount()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        List<HttpStatusCode> made = [];
        for (int booking = 0; booking < 3; booking++)
        {
            using HttpResponseMessage response = await BookAsync(client, Address, booking, "anna@example.com");
            made.Add(response.StatusCode);
        }

        using HttpResponseMessage refusedByTheEmail = await BookAsync(client, Address, 3, "anna@example.com");

        // Act
        using HttpResponseMessage fifth = await BookAsync(client, Address, 4, Email(4));
        using HttpResponseMessage sixth = await BookAsync(client, Address, 5, Email(5));

        // Assert
        Assert.All(made, status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, refusedByTheEmail.StatusCode);
        Assert.Null(refusedByTheEmail.Headers.RetryAfter);
        Assert.Equal(HttpStatusCode.OK, fifth.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, sixth.StatusCode);
        Assert.NotNull(sixth.Headers.RetryAfter);
        Assert.Equal(4, factory.World.Bookings.Items.Count);
    }

    /// <summary>Equivalence (criterion 3, ADR-0051 decision 3): staff can always book, also for an email at its limit.</summary>
    [Fact]
    public async Task Booking_ByStaffForAnEmailAtItsLimit_IsMade()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        for (int booking = 0; booking < 3; booking++)
        {
            using HttpResponseMessage made = await BookAsync(client, $"203.0.113.{30 + booking}", booking, "anna@example.com");
        }

        Room room = factory.World.Rooms.Items.First(candidate => candidate.Size == RoomSize.Double);
        JsonObject body = new()
        {
            ["checkInDate"] = Day(FirstStay.AddDays(300)),
            ["checkOutDate"] = Day(FirstStay.AddDays(302)),
            ["roomId"] = room.Id,
            ["guestId"] = null,
            ["newGuest"] = GuestBody("anna@example.com"),
        };
        using HttpClient staff = factory.CreateAuthorizedClient();

        // Act
        using HttpResponseMessage response = await PostAsync(staff, StaffBookingPath, body, "203.0.113.34");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(4, factory.World.Bookings.Items.Count);
    }

    #endregion

    #region The address of the caller

    /// <summary>
    /// Positive (criterion 4, ADR-0051 decision 4): a call with the service token is audited under the address that the
    /// web application names, not under a forwarded address.
    /// </summary>
    [Fact]
    public async Task StaffSignIn_WithTheServiceToken_IsAuditedUnderTheNamedAddress()
    {
        // Arrange
        await using Factory factory = new();
        factory.AddStaff("web-anna", Password, StaffRole.Manager);
        using HttpClient client = factory.CreateServiceClient();
        using HttpRequestMessage request = SignInRequest("web-anna", "172.18.0.5");
        request.Headers.Add(ApiAuthentication.ClientAddressHeader, "203.0.113.30");
        request.Headers.Add("X-Forwarded-For", "198.51.100.1");

        // Act
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("203.0.113.30", Assert.Single(factory.World.StaffAudit.Events).RemoteAddress);
    }

    /// <summary>
    /// Security (criterion 4): a call without the service token, such as one of the mobile app, cannot name its address;
    /// it is audited under the address that nginx added to the forwarded header.
    /// </summary>
    [Fact]
    public async Task StaffSignIn_WithoutTheServiceToken_CannotNameTheAddress()
    {
        // Arrange
        await using Factory factory = new();
        factory.AddStaff("phone-anna", Password, StaffRole.Manager);
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = SignInRequest("phone-anna", "172.18.0.9");
        request.Headers.Add(ApiAuthentication.ClientAddressHeader, "198.51.100.66");
        request.Headers.Add("X-Forwarded-For", "203.0.113.20");

        // Act
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("203.0.113.20", Assert.Single(factory.World.StaffAudit.Events).RemoteAddress);
    }

    /// <summary>Security (criterion 4): forwarded headers from a peer outside the networks of the stack are ignored.</summary>
    [Fact]
    public async Task StaffSignIn_ForwardedFromOutsideTheStack_IsAuditedUnderThePeer()
    {
        // Arrange
        await using Factory factory = new();
        factory.AddStaff("outside-anna", Password, StaffRole.Manager);
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = SignInRequest("outside-anna", "198.51.100.200");
        request.Headers.Add("X-Forwarded-For", "203.0.113.21");

        // Act
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("198.51.100.200", Assert.Single(factory.World.StaffAudit.Events).RemoteAddress);
    }

    /// <summary>
    /// Security (criterion 4): from a peer of the stack only the last forwarded address counts, the one that nginx added;
    /// an address that the caller wrote in front of it is ignored.
    /// </summary>
    [Fact]
    public async Task StaffSignIn_ForwardedChainFromTheStack_IsAuditedUnderTheLastEntry()
    {
        // Arrange
        await using Factory factory = new();
        factory.AddStaff("chain-anna", Password, StaffRole.Manager);
        using HttpClient client = factory.CreateClient();
        using HttpRequestMessage request = SignInRequest("chain-anna", "172.18.0.9");
        request.Headers.Add("X-Forwarded-For", "198.51.100.77, 203.0.113.22");

        // Act
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("203.0.113.22", Assert.Single(factory.World.StaffAudit.Events).RemoteAddress);
    }

    #endregion

    #region The security events

    /// <summary>
    /// Security (criterion 6, ADR-0051 decision 6): a booking made on the public site writes one event with the address
    /// and the email masked to its first character and its domain, and no other value that the guest typed.
    /// </summary>
    [Fact]
    public async Task Booking_Made_WritesOneEventWithTheAddressAndTheMaskedEmail()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();

        // Act
        using HttpResponseMessage response = await BookAsync(client, "203.0.113.40", 0, "anna.jensen@example.com");
        string answer = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        RecordingLoggerProvider.LogEntry entry = Assert.Single(factory.Logs.Entries, entry => entry.Category == SecurityCategory
            && entry.Message.StartsWith(SecurityEvents.PublicBookingMadeKind, StringComparison.Ordinal));
        Assert.Contains("203.0.113.40", entry.AllText, StringComparison.Ordinal);
        Assert.Contains("a***@example.com", entry.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("anna.jensen", entry.AllText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Jensen", entry.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("12345678", entry.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("Denmark", entry.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain(ReferenceOf(answer), entry.AllText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Security (criterion 6): a guest sign-in with a wrong email writes one event with the masked email that was tried,
    /// and never the reference.
    /// </summary>
    [Fact]
    public async Task GuestSignIn_WithAWrongEmail_WritesOneEventWithoutTheReference()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        using HttpResponseMessage booked = await BookAsync(client, "203.0.113.41", 0, "anna@example.com");
        string reference = ReferenceOf(await booked.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        // Act
        using HttpResponseMessage refused = await PostAsync(
            client,
            GuestSignInPath,
            new JsonObject { ["reference"] = reference, ["email"] = "eve@example.org" },
            "203.0.113.42");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        RecordingLoggerProvider.LogEntry entry = Assert.Single(factory.Logs.Entries, entry => entry.Category == SecurityCategory
            && entry.Message.StartsWith(SecurityEvents.GuestLookupFailedKind, StringComparison.Ordinal));
        Assert.Contains("203.0.113.42", entry.AllText, StringComparison.Ordinal);
        Assert.Contains("e***@example.org", entry.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain(reference, entry.AllText, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Positive (criterion 6): each refusal by a limit writes one event with the name of its limit: the search, the hour,
    /// the email and the guest sign-in.
    /// </summary>
    [Fact]
    public async Task Refusals_OfEveryLimit_WriteOneEventEachWithTheName()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        await SearchManyAsync(client, "203.0.113.50", 61);
        await BookManyAsync(client, "203.0.113.51", 0, 6);
        for (int booking = 0; booking < 4; booking++)
        {
            using HttpResponseMessage response = await BookAsync(client, $"203.0.113.{60 + booking}", 10 + booking, "eve@example.com");
        }

        // Act
        for (int attempt = 0; attempt < 6; attempt++)
        {
            using HttpResponseMessage response = await PostAsync(
                client,
                GuestSignInPath,
                new JsonObject { ["reference"] = "3f2504e0-4f89-41d3-9a0c-0305e82c3301", ["email"] = "eve@example.com" },
                "203.0.113.70");
        }

        // Assert
        string[] limits = factory.Logs.Entries
            .Where(entry => entry.Category == SecurityCategory && entry.Message.StartsWith(SecurityEvents.LimitRefusedKind, StringComparison.Ordinal))
            .Select(entry => entry.Values.Single(value => value.Key == "Limit").Value?.ToString() ?? string.Empty)
            .ToArray();
        Assert.Equal([LimitNames.Search, LimitNames.BookingHour, LimitNames.Email, LimitNames.GuestSignIn], limits);
    }

    /// <summary>
    /// Positive (criterion 6): each event counts once in <c>nfhotel.security.events</c> with its kind, and a refusal with
    /// the name of its limit.
    /// </summary>
    [Fact]
    public async Task Events_OfTheBookingFlow_AreCountedOnceWithTheirKindAndLimit()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        IMeterFactory meters = factory.Services.GetRequiredService<IMeterFactory>();
        ConcurrentQueue<string> counted = new();
        using MeterListener listener = new()
        {
            InstrumentPublished = (instrument, owner) =>
            {
                if (instrument.Meter.Scope == meters && instrument.Name == SecurityMetrics.EventsCounterName)
                {
                    owner.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            string kind = string.Empty;
            string limit = string.Empty;
            foreach (KeyValuePair<string, object?> tag in tags)
            {
                if (tag.Key == SecurityMetrics.KindTag)
                {
                    kind = tag.Value?.ToString() ?? string.Empty;
                }
                else if (tag.Key == SecurityMetrics.LimitTag)
                {
                    limit = tag.Value?.ToString() ?? string.Empty;
                }
            }

            for (long count = 0; count < value; count++)
            {
                counted.Enqueue($"{kind}/{limit}");
            }
        });
        listener.Start();

        // Act
        using HttpResponseMessage booked = await BookAsync(client, "203.0.113.80", 0, "anna@example.com");
        using HttpResponseMessage failed = await PostAsync(
            client,
            GuestSignInPath,
            new JsonObject { ["reference"] = "3f2504e0-4f89-41d3-9a0c-0305e82c3301", ["email"] = "anna@example.com" },
            "203.0.113.81");
        await SearchManyAsync(client, "203.0.113.82", 61);

        // Assert
        Assert.Equal(
            ["guest_lookup_failed/", "limit_refused/search", "public_booking_made/"],
            counted.Order(StringComparer.Ordinal).ToArray());
    }

    /// <summary>Positive (criterion 6): the meter of the counter is one that the telemetry of the services sends on.</summary>
    [Fact]
    public void Telemetry_Meters_IncludeTheMeterOfTheSecurityEvents()
    {
        // Act and assert
        Assert.Contains(SecurityMetrics.MeterName, TelemetryRegistration.Meters);
    }

    #endregion

    #region Helpers

    private static async Task<HttpResponseMessage> SearchAsync(HttpClient client, string address)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            $"{AvailableRoomsPath}?checkInDate={Day(FirstStay)}&checkOutDate={Day(FirstStay.AddDays(2))}");
        request.Headers.Add(ApiAuthentication.ClientAddressHeader, address);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<HttpStatusCode[]> SearchManyAsync(HttpClient client, string address, int count)
    {
        List<HttpStatusCode> statuses = [];
        for (int search = 0; search < count; search++)
        {
            using HttpResponseMessage response = await SearchAsync(client, address);
            statuses.Add(response.StatusCode);
        }

        return [.. statuses];
    }

    private static Task<HttpResponseMessage> BookAsync(HttpClient client, string address, int stay, string email) =>
        PostAsync(client, PublicBookingPath, PublicBody(stay, email), address);

    // Books stays first to first + count - 1, each with an email of its own, so only the limits per address count.
    private static async Task<HttpStatusCode[]> BookManyAsync(HttpClient client, string address, int first, int count)
    {
        List<HttpStatusCode> statuses = [];
        for (int stay = first; stay < first + count; stay++)
        {
            using HttpResponseMessage response = await BookAsync(client, address, stay, Email(stay));
            statuses.Add(response.StatusCode);
        }

        return [.. statuses];
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path, JsonObject body, string address)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(ApiAuthentication.ClientAddressHeader, address);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static HttpRequestMessage SignInRequest(string userName, string peer)
    {
        HttpRequestMessage request = new(HttpMethod.Post, StaffSignInPath)
        {
            Content = JsonContent.Create(new SignInRequest(userName, Password), options: ApiJson.Options),
        };
        request.Headers.Add(PeerHeader, peer);
        return request;
    }

    // A stay of its own for each number, three days after the one before, so no two stays want the same room.
    private static JsonObject PublicBody(int stay, string email) => new()
    {
        ["checkInDate"] = Day(FirstStay.AddDays(3 * stay)),
        ["checkOutDate"] = Day(FirstStay.AddDays((3 * stay) + 2)),
        ["roomSize"] = (int)RoomSize.Double,
        ["guests"] = 1,
        ["newGuest"] = GuestBody(email),
    };

    private static JsonObject GuestBody(string email) => new()
    {
        ["firstName"] = "Anna",
        ["lastName"] = "Jensen",
        ["email"] = email,
        ["phoneNumber"] = "+4512345678",
        ["country"] = "Denmark",
        ["passportNumber"] = null,
    };

    private static string Email(int number) => $"guest{number.ToString(CultureInfo.InvariantCulture)}@example.com";

    private static string Day(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static async Task<string[]> CodesAsync(HttpResponseMessage response)
    {
        JsonArray? errors = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))?["errors"]?.AsArray();
        return errors is null ? [] : [.. errors.Select(error => error?.GetValue<string>() ?? string.Empty)];
    }

    private static string ReferenceOf(string answer) =>
        JsonNode.Parse(answer)?["value"]?["receipt"]?["reference"]?.GetValue<string>()
        ?? throw new InvalidOperationException("The answer holds no reference.");

    #endregion

    /// <summary>
    /// The API with ten rooms of each size that sleep four, the numbers of the table "Limits of the booking flow", the
    /// windows on the clock of the test world, a record of every log entry, and the peer of the connection that a test
    /// names.
    /// </summary>
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
        protected override IEnumerable<KeyValuePair<string, string>> ExtraSettings() =>
        [
            new("SenderLimits:SearchesPerMinute", "60"),
            new("SenderLimits:BookingRequestsPerHour", "5"),
            new("SenderLimits:BookingRequestsPerDay", "20"),
            new("SenderLimits:ComingPublicBookingsPerEmail", "3"),
            new("SenderLimits:MaxTrackedKeys", "10000"),
        ];

        /// <inheritdoc />
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureLogging(logging => logging.AddProvider(Logs));
            builder.ConfigureServices(services =>
            {
                // The windows of the limits run on the clock of the world, which a test moves (criterion 3).
                services.RemoveAll<ISenderLimits>();
                services.AddSingleton<ISenderLimits>(provider =>
                    new InMemorySenderLimits(provider.GetRequiredService<SenderLimitOptions>(), World.Clock));
                services.AddSingleton<IStartupFilter>(new PeerStartupFilter());
            });
        }

        /// <summary>Gives a request the peer that a test names in <see cref="PeerHeader"/>, before the pipeline of the API.</summary>
        private sealed class PeerStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
            {
                app.Use(async (context, nextStep) =>
                {
                    if (IPAddress.TryParse(context.Request.Headers[PeerHeader].ToString(), out IPAddress? peer))
                    {
                        context.Connection.RemoteIpAddress = peer;
                    }

                    await nextStep(context);
                });
                next(app);
            };
        }
    }
}
