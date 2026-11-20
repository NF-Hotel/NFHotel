using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Api.Tests;

/// <summary>
/// The booking request and its verification by email through the API (MIL-088 criteria 3 to 6 and 8, ADR-0052 and
/// ADR-0053): the mode, the status and the codes of each answer, the token in the body only, the limits per mailbox and
/// per address, the fuse, a relay that fails, and the security events with their counters. Each test starts a host of
/// its own over a world of its own.
/// </summary>
public sealed class BookingRequestApiTests
{
    private const string ModePath = "api/bookings/public/mode";
    private const string AskPath = "api/bookings/public/requests";
    private const string VerifyPath = "api/bookings/public/requests/verify";
    private const string PublicBookingPath = "api/bookings/public";
    private const string SecurityCategory = "NFHotel.Security";
    private const string Address = "203.0.113.20";
    private const string Email = "anna@example.com";

    private static readonly DateOnly Stay = new(2026, 3, 1);

    #region The mode and the setting

    /// <summary>Positive (criterion 8, ADR-0052 decision 8): with the setting off the mode says that the public site books at once.</summary>
    [Fact]
    public async Task Mode_VerificationOff_SaysThatTheSiteBooksAtOnce()
    {
        // Arrange
        await using Factory factory = new(verification: false);
        using HttpClient client = factory.CreateServiceClient();

        // Act
        using HttpResponseMessage response = await client.GetAsync(ModePath, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False((await ValueAsync(response))?["verificationRequired"]?.GetValue<bool>());
    }

    /// <summary>Positive (ADR-0052 decision 8): with the setting on the mode says that a booking is asked for and verified.</summary>
    [Fact]
    public async Task Mode_VerificationOn_SaysThatABookingIsVerified()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();

        // Act
        using HttpResponseMessage response = await client.GetAsync(ModePath, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True((await ValueAsync(response))?["verificationRequired"]?.GetValue<bool>());
    }

    /// <summary>
    /// Negative (criterion 8): with the setting off the request and the verification answer 404, so the old path is the
    /// only one, and the public booking books at once as before.
    /// </summary>
    [Fact]
    public async Task RequestAndVerify_VerificationOff_AreNotFoundAndThePublicBookingBooks()
    {
        // Arrange
        await using Factory factory = new(verification: false);
        using HttpClient client = factory.CreateServiceClient();

        // Act
        using HttpResponseMessage asked = await PostAsync(client, AskPath, BookingBody(Email));
        using HttpResponseMessage verified = await PostAsync(client, VerifyPath, new JsonObject { ["token"] = new string('a', 43) });
        using HttpResponseMessage booked = await PostAsync(client, PublicBookingPath, BookingBody(Email));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, asked.StatusCode);
        Assert.Equal([ErrorCodes.Booking.NotFound], await CodesAsync(asked));
        Assert.Equal(HttpStatusCode.NotFound, verified.StatusCode);
        Assert.Equal(HttpStatusCode.OK, booked.StatusCode);
        Assert.Empty(factory.World.Mails.Attempts);
    }

    /// <summary>
    /// Security (ADR-0052 decision 8): with the setting on the public booking at once answers 409, so no caller books
    /// around the verification, and nothing is stored.
    /// </summary>
    [Fact]
    public async Task PublicBooking_VerificationOn_IsRefusedAndStoresNothing()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();

        // Act
        using HttpResponseMessage response = await PostAsync(client, PublicBookingPath, BookingBody(Email));

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal([ErrorCodes.Booking.VerificationRequired], await CodesAsync(response));
        Assert.Empty(factory.World.Bookings.Items);
        Assert.Empty(factory.World.Guests.Items);
    }

    /// <summary>
    /// Negative (criterion 8, ADR-0052 decision 8): with the setting on and no relay set the API refuses to start and
    /// names the setting that is missing.
    /// </summary>
    [Fact]
    public void Start_VerificationOnWithoutARelay_IsRefusedWithTheNameOfTheSetting()
    {
        // Arrange
        using SettingsFactory factory = new(
            new("BookingVerification:Enabled", "true"),
            new("BookingVerification:SiteAddress", "https://hotel.example.com/"));

        // Act
        Exception refused = Assert.ThrowsAny<Exception>(() => factory.CreateServiceClient());

        // Assert
        Assert.Contains("'Smtp:Host' must be set", refused.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Security (criterion 6, ADR-0053 decision 2): a relay without an encrypted connection is refused at the start, with
    /// the name of the setting and without the value of the password.
    /// </summary>
    [Fact]
    public void Start_RelayWithoutEncryption_IsRefusedWithTheNameOfTheSetting()
    {
        // Arrange
        using SettingsFactory factory = new(
            new("Smtp:Security", "None"),
            new("Smtp:Password", "relay-password-of-the-test"));

        // Act
        Exception refused = Assert.ThrowsAny<Exception>(() => factory.CreateServiceClient());

        // Assert
        string text = refused.ToString();
        Assert.Contains("'Smtp:Security' must be SslOnConnect or StartTls", text, StringComparison.Ordinal);
        Assert.DoesNotContain("relay-password-of-the-test", text, StringComparison.Ordinal);
    }

    #endregion

    #region Ask

    /// <summary>
    /// Positive (criterion 3, ADR-0052 decision 1): a request answers 202 with the time the link lapses, sends one mail to
    /// the email of the request, takes no room and stores no guest and no booking.
    /// </summary>
    [Fact]
    public async Task Ask_ValidRequest_Answers202SendsTheMailAndTakesNoRoom()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();

        // Act
        using HttpResponseMessage response = await PostAsync(client, AskPath, BookingBody(Email));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        DateTimeOffset expiresAt = DateTimeOffset.Parse(
            (await ValueAsync(response))?["expiresAt"]?.GetValue<string>() ?? string.Empty,
            CultureInfo.InvariantCulture);
        Assert.Equal(factory.World.Clock.UtcNow.AddMinutes(30), expiresAt);
        OutgoingMail mail = Assert.Single(factory.World.Mails.Sent);
        Assert.Equal(Email, mail.To);
        Assert.Single(factory.World.BookingRequests.Items);
        Assert.Empty(factory.World.Bookings.Items);
        Assert.Empty(factory.World.Guests.Items);
    }

    /// <summary>
    /// Security (criterion 6, ADR-0053 decision 5): with the relay down the guest is told with 503 and the code, no request
    /// is left, and the event names the kind of the mail and the reason in a word, at the level Error.
    /// </summary>
    [Fact]
    public async Task Ask_RelayDown_Answers503LeavesNoRequestAndWritesTheEvent()
    {
        // Arrange
        await using Factory factory = new();
        factory.World.Mails.RefuseAll(MailFailureReasons.Connect);
        using HttpClient client = factory.CreateServiceClient();

        // Act
        using HttpResponseMessage response = await PostAsync(client, AskPath, BookingBody(Email));

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal([ErrorCodes.Booking.MailNotSent], await CodesAsync(response));
        Assert.Empty(factory.World.BookingRequests.Items);
        Assert.Empty(factory.World.SentMails.Items);
        RecordingLoggerProvider.LogEntry entry = Assert.Single(factory.EventsOf(SecurityEvents.MailNotSentKind));
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("mail_not_sent verification_link connect", entry.Message);
    }

    /// <summary>
    /// Security (ADR-0053 decision 6): with the fuse of the mails a day used up a request is refused with 503 and its code,
    /// no mail is tried, and the event is written.
    /// </summary>
    [Fact]
    public async Task Ask_FuseUsedUp_Answers503AndTriesNoMail()
    {
        // Arrange
        await using Factory factory = new();
        factory.World.Verification.MaxMailsPerDay = 1;
        factory.World.SentMails.AddSent("eve@example.com", MailKind.VerificationLink, factory.World.Clock.UtcNow.AddHours(-1));
        using HttpClient client = factory.CreateServiceClient();

        // Act
        using HttpResponseMessage response = await PostAsync(client, AskPath, BookingBody(Email));

        // Assert
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal([ErrorCodes.Booking.MailPaused], await CodesAsync(response));
        Assert.Empty(factory.World.Mails.Attempts);
        Assert.Single(factory.EventsOf(SecurityEvents.MailFuseBlownKind));
    }

    /// <summary>
    /// Boundary (criterion 5, ADR-0052 decision 6): the fourth verification mail to one mailbox within an hour is refused
    /// with 429 and its own code, without <c>Retry-After</c>, and the event names the limit and holds the masked email.
    /// The forms of one Gmail mailbox count as one.
    /// </summary>
    [Fact]
    public async Task Ask_FourthMailToOneMailboxInAnHour_IsRefusedWithItsOwnCode()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        HttpStatusCode[] allowed =
        [
            await StatusOfAsync(client, AskPath, BookingBody("anna.jensen@gmail.com")),
            await StatusOfAsync(client, AskPath, BookingBody("annajensen+trip@gmail.com")),
            await StatusOfAsync(client, AskPath, BookingBody("Anna.Jensen@googlemail.com")),
        ];

        // Act
        using HttpResponseMessage refused = await PostAsync(client, AskPath, BookingBody("annajensen@gmail.com"));

        // Assert
        Assert.All(allowed, status => Assert.Equal(HttpStatusCode.Accepted, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal([ErrorCodes.Booking.TooManyMails], await CodesAsync(refused));
        Assert.Null(refused.Headers.RetryAfter);
        RecordingLoggerProvider.LogEntry entry = Assert.Single(factory.EventsOf(SecurityEvents.LimitRefusedKind));
        Assert.Equal($"limit_refused {LimitNames.VerificationMail} {Address} a***@gmail.com", entry.Message);
    }

    /// <summary>
    /// Boundary (ADR-0052 decision 1): the limits per address of MIL-087 count a request as a booking request, so the
    /// request over the number of the hour is refused with the code of the limits per address and <c>Retry-After</c>.
    /// </summary>
    [Fact]
    public async Task Ask_OverTheBookingRequestsOfTheAddress_IsRefusedByTheLimitPerAddress()
    {
        // Arrange
        await using Factory factory = new(bookingRequestsPerHour: 1);
        using HttpClient client = factory.CreateServiceClient();
        using HttpResponseMessage first = await PostAsync(client, AskPath, BookingBody(Email));

        // Act
        using HttpResponseMessage second = await PostAsync(client, AskPath, BookingBody("eve@example.com"));

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal([ErrorCodes.Booking.TooManyRequests], await CodesAsync(second));
        Assert.NotNull(second.Headers.RetryAfter);
        Assert.Single(factory.World.Mails.Sent);
    }

    #endregion

    #region Verify

    /// <summary>
    /// Positive (criterion 4, ADR-0052 decision 5): the token of the mail books the request: the answer holds the receipt
    /// with its reference and the guest session, the booking is stored with its guest, a second mail carries the
    /// reference, and the events of the request and of the booking are written with the masked email.
    /// </summary>
    [Fact]
    public async Task Verify_TokenOfTheMail_BooksSignsTheGuestInAndSendsTheReceipt()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        using HttpResponseMessage asked = await PostAsync(client, AskPath, BookingBody(Email));

        // Act
        using HttpResponseMessage verified = await PostAsync(client, VerifyPath, new JsonObject { ["token"] = factory.World.Mails.LastToken() });

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, asked.StatusCode);
        Assert.Equal(HttpStatusCode.OK, verified.StatusCode);
        JsonNode? value = await ValueAsync(verified);
        Booking booking = Assert.Single(factory.World.Bookings.Items);
        Assert.Equal(booking.Reference.ToString(), value?["receipt"]?["reference"]?.GetValue<string>());
        Assert.False(string.IsNullOrEmpty(value?["session"]?["token"]?.GetValue<string>()));
        Assert.True(booking.MadeOnPublicSite);
        Assert.Single(factory.World.Guests.Items);
        Assert.Equal(2, factory.World.Mails.Sent.Count);
        Assert.Contains(booking.Reference.ToString(), factory.World.Mails.Sent[^1].Body, StringComparison.Ordinal);
        Assert.Equal($"booking_request_made {Address} a***@example.com", Assert.Single(factory.EventsOf(SecurityEvents.BookingRequestMadeKind)).Message);
        Assert.Equal($"public_booking_made {Address} a***@example.com", Assert.Single(factory.EventsOf(SecurityEvents.PublicBookingMadeKind)).Message);
    }

    /// <summary>
    /// Security (criterion 3, ADR-0052 decision 3): a used token gets the answer of a link that does not work, 422 with its
    /// code, and the event with the address only.
    /// </summary>
    [Fact]
    public async Task Verify_SameTokenTwice_SecondGetsTheAnswerOfALinkThatDoesNotWork()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        using HttpResponseMessage asked = await PostAsync(client, AskPath, BookingBody(Email));
        JsonObject body = new() { ["token"] = factory.World.Mails.LastToken() };
        using HttpResponseMessage first = await PostAsync(client, VerifyPath, body);

        // Act
        using HttpResponseMessage second = await PostAsync(client, VerifyPath, body);

        // Assert
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
        Assert.Equal([ErrorCodes.Booking.VerificationLinkInvalid], await CodesAsync(second));
        Assert.Equal($"verification_failed {Address}", Assert.Single(factory.EventsOf(SecurityEvents.VerificationFailedKind)).Message);
        Assert.Single(factory.World.Bookings.Items);
    }

    /// <summary>
    /// Security (ADR-0052 decision 3): a token longer than its limit, a body with a field of its own and a body without the
    /// token are refused with 400 before a use case runs, and no request is claimed.
    /// </summary>
    /// <param name="body">The body of the verification.</param>
    [Theory]
    [InlineData("{\"token\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"}")]
    [InlineData("{\"token\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"email\":\"anna@example.com\"}")]
    [InlineData("{}")]
    public async Task Verify_BodyOfTheWrongShape_IsRefusedBeforeTheUseCase(string body)
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        using HttpResponseMessage asked = await PostAsync(client, AskPath, BookingBody(Email));
        using HttpRequestMessage request = new(HttpMethod.Post, VerifyPath)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        // Act
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single(factory.World.BookingRequests.Items);
        Assert.Empty(factory.World.BookingRequests.Claimed);
    }

    #endregion

    #region Counters

    /// <summary>
    /// Positive (ADR-0052 decision 10, ADR-0053 decision 6): the events of the request count once in
    /// <c>nfhotel.security.events</c> with their kind, and each mail that the relay took counts once in
    /// <c>nfhotel.mails</c> with its kind.
    /// </summary>
    [Fact]
    public async Task Events_OfTheRequestAndItsMails_AreCountedOnceWithTheirKind()
    {
        // Arrange
        await using Factory factory = new();
        using HttpClient client = factory.CreateServiceClient();
        IMeterFactory meters = factory.Services.GetRequiredService<IMeterFactory>();
        ConcurrentQueue<string> counted = new();
        using MeterListener listener = CountingListener(meters, counted);
        listener.Start();

        // Act
        using HttpResponseMessage asked = await PostAsync(client, AskPath, BookingBody(Email));
        using HttpResponseMessage verified = await PostAsync(client, VerifyPath, new JsonObject { ["token"] = factory.World.Mails.LastToken() });
        using HttpResponseMessage failed = await PostAsync(client, VerifyPath, new JsonObject { ["token"] = new string('b', 43) });
        factory.World.Mails.RefuseAll(MailFailureReasons.Timeout);
        using HttpResponseMessage notSent = await PostAsync(client, AskPath, BookingBody("eve@example.com"));

        // Assert
        Assert.Equal(
            [
                "nfhotel.mails/booking_receipt",
                "nfhotel.mails/verification_link",
                "nfhotel.security.events/booking_request_made",
                "nfhotel.security.events/mail_not_sent",
                "nfhotel.security.events/public_booking_made",
                "nfhotel.security.events/verification_failed",
            ],
            counted.Order(StringComparer.Ordinal).ToArray());
    }

    /// <summary>
    /// Positive (ADR-0053 decisions 5 and 6): once the API has started, the series of the mails that the alerts read stand
    /// at 0, so the first mail that fails is an increase that the alert sees.
    /// </summary>
    [Fact]
    public async Task Start_SeriesOfTheMails_StandAtZero()
    {
        // Arrange
        ConcurrentQueue<string> zeros = new();
        using MeterListener listener = new()
        {
            InstrumentPublished = (instrument, owner) =>
            {
                if (instrument.Meter.Name == SecurityMetrics.MeterName)
                {
                    owner.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            if (value == 0)
            {
                zeros.Enqueue($"{instrument.Name}/{KindOf(tags)}");
            }
        });
        listener.Start();
        await using Factory factory = new();

        // Act
        using HttpClient client = factory.CreateServiceClient();

        // Assert
        Assert.Superset(
            new HashSet<string>(
                [
                    "nfhotel.mails/booking_receipt",
                    "nfhotel.mails/verification_link",
                    "nfhotel.security.events/mail_fuse_blown",
                    "nfhotel.security.events/mail_not_sent",
                ],
                StringComparer.Ordinal),
            zeros.ToHashSet(StringComparer.Ordinal));
    }

    #endregion

    #region Helpers

    private static MeterListener CountingListener(IMeterFactory meters, ConcurrentQueue<string> counted)
    {
        MeterListener listener = new()
        {
            InstrumentPublished = (instrument, owner) =>
            {
                if (instrument.Meter.Scope == meters
                    && instrument.Name is SecurityMetrics.EventsCounterName or SecurityMetrics.MailsCounterName)
                {
                    owner.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            for (long count = 0; count < value; count++)
            {
                counted.Enqueue($"{instrument.Name}/{KindOf(tags)}");
            }
        });
        return listener;
    }

    private static string KindOf(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        foreach (KeyValuePair<string, object?> tag in tags)
        {
            if (tag.Key == SecurityMetrics.KindTag)
            {
                return tag.Value?.ToString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string path, JsonObject body)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, path)
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(ApiAuthentication.ClientAddressHeader, Address);
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<HttpStatusCode> StatusOfAsync(HttpClient client, string path, JsonObject body)
    {
        using HttpResponseMessage response = await PostAsync(client, path, body);
        return response.StatusCode;
    }

    private static JsonObject BookingBody(string email) => new()
    {
        ["checkInDate"] = Day(Stay),
        ["checkOutDate"] = Day(Stay.AddDays(2)),
        ["roomSize"] = (int)RoomSize.Double,
        ["guests"] = 1,
        ["newGuest"] = new JsonObject
        {
            ["firstName"] = "Anna",
            ["lastName"] = "Jensen",
            ["email"] = email,
            ["phoneNumber"] = "+4512345678",
            ["country"] = "Denmark",
            ["passportNumber"] = null,
        },
    };

    private static string Day(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static async Task<JsonNode?> ValueAsync(HttpResponseMessage response) =>
        JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))?["value"];

    private static async Task<string[]> CodesAsync(HttpResponseMessage response)
    {
        JsonArray? errors = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))?["errors"]?.AsArray();
        return errors is null ? [] : [.. errors.Select(error => error?.GetValue<string>() ?? string.Empty)];
    }

    #endregion

    /// <summary>
    /// The API with three rooms that sleep four, the verification by email on unless a test switches it off, the limits
    /// per address of the table "Limits of the booking flow" unless a test sets the booking requests of an hour, and a
    /// record of every log entry.
    /// </summary>
    public sealed class Factory : ApiFactory
    {
        private readonly int _bookingRequestsPerHour;

        /// <summary>Creates the API.</summary>
        /// <param name="verification">Whether the verification by email is on.</param>
        /// <param name="bookingRequestsPerHour">The booking requests that one address may send in an hour.</param>
        public Factory(bool verification = true, int bookingRequestsPerHour = 5)
        {
            _bookingRequestsPerHour = bookingRequestsPerHour;
            World.Verification.Enabled = verification;
            for (int number = 0; number < 3; number++)
            {
                World.AddRoom((101 + number).ToString(CultureInfo.InvariantCulture), 1, RoomSize.Double, 4);
            }
        }

        /// <summary>The log entries that the API wrote.</summary>
        public RecordingLoggerProvider Logs { get; } = new();

        /// <summary>The security events of one kind, oldest first.</summary>
        /// <param name="kind">The kind of the event.</param>
        /// <returns>The entries.</returns>
        public IReadOnlyList<RecordingLoggerProvider.LogEntry> EventsOf(string kind) =>
        [
            .. Logs.Entries.Where(entry => entry.Category == SecurityCategory
                && (entry.Message == kind || entry.Message.StartsWith(kind + " ", StringComparison.Ordinal))),
        ];

        /// <inheritdoc />
        protected override IEnumerable<KeyValuePair<string, string>> ExtraSettings() =>
        [
            new("SenderLimits:BookingRequestsPerHour", _bookingRequestsPerHour.ToString(CultureInfo.InvariantCulture)),
            new("SenderLimits:BookingRequestsPerDay", "20"),
            new("SenderLimits:ComingPublicBookingsPerEmail", "3"),
        ];

        /// <inheritdoc />
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureLogging(logging => logging.AddProvider(Logs));
        }
    }

    /// <summary>The API with settings of its own, to show what the start refuses.</summary>
    /// <param name="settings">The settings over those of every test.</param>
    private sealed class SettingsFactory(params KeyValuePair<string, string>[] settings) : ApiFactory
    {
        /// <inheritdoc />
        protected override IEnumerable<KeyValuePair<string, string>> ExtraSettings() => settings;
    }
}
