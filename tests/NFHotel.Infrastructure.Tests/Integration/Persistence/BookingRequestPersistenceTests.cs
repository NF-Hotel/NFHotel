using NFHotel.Application.Errors;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.Infrastructure.Persistence;
using NFHotel.Infrastructure.Repositories;
using NFHotel.TestSupport;

using Npgsql;

namespace NFHotel.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The booking requests, the records of the mails and the lock of the unit of work against a real PostgreSQL (MIL-088
/// criteria 3 to 5, ADR-0052 decisions 2, 5, 6 and 7): what is stored and how, the claim that a link pressed twice at
/// once gives to one press only, the windows of the counts, the clean-up, the lock that lets one request count at a
/// time, and the count of coming public bookings by mailbox.
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class BookingRequestPersistenceTests : IAsyncLifetime
{
    private const string Passport = "AB1234567";

    private static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateRange Stay = new(new DateOnly(2030, 2, 1), new DateOnly(2030, 2, 3));

    private readonly PostgreSqlFixture _fixture;
    private int _rooms;

    public BookingRequestPersistenceTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>Runs before each test and starts it on an empty database.</summary>
    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    /// <summary>Nothing to clean up: the next test resets the database.</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region The request

    /// <summary>
    /// Integration (ADR-0052 decisions 2 and 7): a request is stored with the hash of its token, the key of its mailbox
    /// and its fields, and its passport number is encrypted in the table as the passport number of a guest is.
    /// </summary>
    [Fact]
    public async Task Add_Request_IsStoredWithItsHashesAndAnEncryptedPassport()
    {
        // Arrange
        BookingRequest request = Request("anna@example.com", "token-1", Now);

        // Act
        await AddAsync(request);
        await using HotelDbContext context = _fixture.CreateContext();
        BookingRequest? read = await new BookingRequestRepository(context)
            .FindByEmailKeyAsync(BookingRequestTestData.KeyOf("anna@example.com"), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(read);
        Assert.Equal(BookingRequestTestData.HashOf("token-1"), read.TokenHash);
        Assert.Equal(new DateOnly(2030, 2, 1), read.StartDate);
        Assert.Equal(new DateOnly(2030, 2, 3), read.EndDate);
        Assert.Equal(RoomSize.Double, read.RoomSize);
        Assert.Equal("anna@example.com", read.Email);
        Assert.Equal(Passport, read.PassportNumber);
        Assert.Equal(Now.AddMinutes(30), read.ExpiresAt);
        string stored = await ScalarAsync("SELECT passport_number FROM booking_request");
        Assert.NotEqual(Passport, stored);
        Assert.DoesNotContain(Passport, stored, StringComparison.Ordinal);
    }

    /// <summary>
    /// Integration (ADR-0052 decision 6): one mailbox has one open request; a second request with the key of the same
    /// mailbox is refused by the unique index, so only a renewal replaces a request.
    /// </summary>
    [Fact]
    public async Task Add_SecondRequestOfTheSameMailbox_IsRefusedByTheUniqueIndex()
    {
        // Arrange
        await AddAsync(Request("anna@example.com", "token-1", Now));

        // Act
        UniqueConstraintViolationException refused = await Assert.ThrowsAsync<UniqueConstraintViolationException>(
            () => AddAsync(Request("Anna@Example.com", "token-2", Now)));

        // Assert
        Assert.Equal("ux_booking_request_email_key", refused.ConstraintName);
    }

    /// <summary>
    /// Integration (criterion 5, ADR-0052 decision 6): a renewal gives the request a new token, so the link of the old one
    /// claims nothing.
    /// </summary>
    [Fact]
    public async Task Renew_Request_MakesTheOldLinkUseless()
    {
        // Arrange
        await AddAsync(Request("anna@example.com", "token-1", Now));
        await using (HotelDbContext context = _fixture.CreateContext())
        {
            // ! is justified: the request of this mailbox was added just above.
            BookingRequest kept = (await new BookingRequestRepository(context)
                .FindByEmailKeyAsync(BookingRequestTestData.KeyOf("anna@example.com"), TestContext.Current.CancellationToken))!;
            kept.Renew(NewGuest(), Stay, RoomSize.Double, 2, BookingRequestTestData.HashOf("token-2"), Now.AddMinutes(5));
            await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act
        BookingRequest? old = await ClaimAsync("token-1", Now.AddMinutes(6));
        BookingRequest? renewed = await ClaimAsync("token-2", Now.AddMinutes(6));

        // Assert
        Assert.Null(old);
        Assert.NotNull(renewed);
        Assert.Equal(2, renewed.Guests);
        Assert.Equal(Now.AddMinutes(35), renewed.ExpiresAt);
    }

    #endregion

    #region The claim

    /// <summary>
    /// Integration (criterion 3, ADR-0052 decision 5): an open link claims its request once: the claim returns the request
    /// with its fields and deletes it, and a second claim of the same token finds nothing.
    /// </summary>
    [Fact]
    public async Task ClaimOpenAsync_OpenRequest_IsClaimedOnceAndDeleted()
    {
        // Arrange
        await AddAsync(Request("anna@example.com", "token-1", Now));

        // Act
        BookingRequest? first = await ClaimAsync("token-1", Now.AddMinutes(29));
        BookingRequest? second = await ClaimAsync("token-1", Now.AddMinutes(29));

        // Assert
        Assert.NotNull(first);
        Assert.Equal(Passport, first.PassportNumber);
        Assert.Equal("Anna", first.FirstName);
        Assert.Null(second);
        Assert.Equal("0", await ScalarAsync("SELECT count(*)::text FROM booking_request"));
    }

    /// <summary>
    /// Boundary (criterion 3, ADR-0052 decision 2): at the moment the link lapses it claims nothing, and the request stays
    /// for the clean-up.
    /// </summary>
    [Fact]
    public async Task ClaimOpenAsync_AtTheMomentTheLinkLapses_ClaimsNothing()
    {
        // Arrange
        await AddAsync(Request("anna@example.com", "token-1", Now));

        // Act
        BookingRequest? claimed = await ClaimAsync("token-1", Now.AddMinutes(30));

        // Assert
        Assert.Null(claimed);
        Assert.Equal("1", await ScalarAsync("SELECT count(*)::text FROM booking_request"));
    }

    /// <summary>
    /// Security (criterion 3, ADR-0052 decision 5, ASVS 2.3.3): ten presses of one link at once, each with a connection of
    /// its own, claim the request once.
    /// </summary>
    [Fact]
    public async Task ClaimOpenAsync_TenPressesAtOnce_ClaimTheRequestOnce()
    {
        // Arrange
        await AddAsync(Request("anna@example.com", "token-1", Now));

        // Act
        BookingRequest?[] claims = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Task.Run(() => ClaimAsync("token-1", Now.AddMinutes(1)))));

        // Assert
        Assert.Single(claims, claim => claim is not null);
    }

    #endregion

    #region The clean-up and the records of the mails

    /// <summary>
    /// Integration (criterion 5, ADR-0052 decision 7): the clean-up deletes the requests whose link has lapsed, with their
    /// personal data, and keeps the open ones.
    /// </summary>
    [Fact]
    public async Task DeleteLapsedAsync_LapsedAndOpenRequests_DeletesTheLapsedOnes()
    {
        // Arrange
        await AddAsync(Request("anna@example.com", "token-1", Now.AddMinutes(-31)));
        await AddAsync(Request("bo@example.com", "token-2", Now.AddMinutes(-30)));
        await AddAsync(Request("eve@example.com", "token-3", Now.AddMinutes(-29)));
        await using HotelDbContext context = _fixture.CreateContext();

        // Act
        int deleted = await new BookingRequestRepository(context).DeleteLapsedAsync(Now, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, deleted);
        Assert.Equal("eve@example.com", await ScalarAsync("SELECT string_agg(email, ',') FROM booking_request"));
    }

    /// <summary>
    /// Boundary (criterion 5, ADR-0052 decision 6): a window counts the mails of one mailbox and one kind whose time is
    /// after its start; a mail at the start of the window no longer counts.
    /// </summary>
    [Fact]
    public async Task CountForEmailSinceAsync_MailsAroundTheWindow_CountsThoseAfterItsStart()
    {
        // Arrange
        string anna = BookingRequestTestData.KeyOf("anna@example.com");
        await AddMailsAsync(
            SentMail.Record(anna, MailKind.VerificationLink, Now.AddHours(-1)),
            SentMail.Record(anna, MailKind.VerificationLink, Now.AddMinutes(-59)),
            SentMail.Record(anna, MailKind.VerificationLink, Now),
            SentMail.Record(anna, MailKind.BookingReceipt, Now),
            SentMail.Record(BookingRequestTestData.KeyOf("eve@example.com"), MailKind.VerificationLink, Now));
        await using HotelDbContext context = _fixture.CreateContext();
        SentMailRepository mails = new(context);

        // Act
        int ofAnna = await mails.CountForEmailSinceAsync(anna, MailKind.VerificationLink, Now.AddHours(-1), TestContext.Current.CancellationToken);
        int all = await mails.CountSinceAsync(Now.AddHours(-1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, ofAnna);
        Assert.Equal(4, all);
    }

    /// <summary>Integration (ADR-0052 decision 7): the clean-up deletes the records of mails of 24 hours ago and older.</summary>
    [Fact]
    public async Task DeleteOlderThanAsync_RecordsAroundTheCutoff_DeletesTheOldOnes()
    {
        // Arrange
        string anna = BookingRequestTestData.KeyOf("anna@example.com");
        await AddMailsAsync(
            SentMail.Record(anna, MailKind.VerificationLink, Now.AddDays(-2)),
            SentMail.Record(anna, MailKind.VerificationLink, Now.AddDays(-1)),
            SentMail.Record(anna, MailKind.VerificationLink, Now.AddHours(-23)));
        await using HotelDbContext context = _fixture.CreateContext();

        // Act
        int deleted = await new SentMailRepository(context).DeleteOlderThanAsync(Now.AddDays(-1), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, deleted);
        Assert.Equal("1", await ScalarAsync("SELECT count(*)::text FROM sent_mail"));
    }

    #endregion

    #region The lock

    /// <summary>
    /// Security (ADR-0052 decision 6): two pieces of work under the same lock, each with a connection of its own, run one
    /// after the other: the second starts only once the first has committed.
    /// </summary>
    [Fact]
    public async Task RunUnderLockAsync_TwoAtOnce_RunOneAfterTheOther()
    {
        // Arrange
        TaskCompletionSource firstHolds = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondRuns = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await using HotelDbContext firstContext = _fixture.CreateContext();
        await using HotelDbContext secondContext = _fixture.CreateContext();
        Task<int> first = new UnitOfWork(firstContext).RunUnderLockAsync(
            "booking_request",
            async _ =>
            {
                firstHolds.TrySetResult();
                await releaseFirst.Task;
                return 1;
            },
            TestContext.Current.CancellationToken);
        await firstHolds.Task.WaitAsync(TestContext.Current.CancellationToken);

        // Act
        Task<int> second = new UnitOfWork(secondContext).RunUnderLockAsync(
            "booking_request",
            _ =>
            {
                secondRuns.TrySetResult();
                return Task.FromResult(2);
            },
            TestContext.Current.CancellationToken);
        bool waited = await SomebodyWaitsForTheLockAsync();
        bool ranWhileHeld = secondRuns.Task.IsCompleted;
        releaseFirst.TrySetResult();

        // Assert
        Assert.True(waited);
        Assert.False(ranWhileHeld);
        int[] results = await Task.WhenAll(first, second);
        Assert.Equal([1, 2], results);
    }

    /// <summary>Negative (ADR-0052 decision 6): work that throws under the lock stores nothing of what it saved.</summary>
    [Fact]
    public async Task RunUnderLockAsync_WorkThrows_StoresNothing()
    {
        // Arrange
        await using HotelDbContext context = _fixture.CreateContext();
        UnitOfWork unitOfWork = new(context);

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.RunUnderLockAsync<int>(
            "booking_request",
            async token =>
            {
                new SentMailRepository(context).Add(SentMail.Record(BookingRequestTestData.KeyOf("anna@example.com"), MailKind.VerificationLink, Now));
                await unitOfWork.SaveChangesAsync(token);
                throw new InvalidOperationException("The work failed after its save.");
            },
            TestContext.Current.CancellationToken));

        // Assert
        Assert.Equal("0", await ScalarAsync("SELECT count(*)::text FROM sent_mail"));
    }

    #endregion

    #region The count by mailbox

    /// <summary>
    /// Integration (ADR-0052 decision 6): the limit of coming public bookings counts by mailbox, so the forms of one Gmail
    /// mailbox count once each booking, and the same name at another domain does not count.
    /// </summary>
    [Fact]
    public async Task CountComingPublicBookingsAsync_FormsOfOneGmailMailbox_CountTogether()
    {
        // Arrange
        DateOnly today = new(2030, 1, 1);
        await SeedBookingAsync("A.nna+trip@googlemail.com", today.AddDays(10));
        await SeedBookingAsync("ANNA@gmail.com", today.AddDays(20));
        await SeedBookingAsync("anna@example.com", today.AddDays(30));
        await SeedBookingAsync("an_na@gmail.com", today.AddDays(40));
        await using HotelDbContext context = _fixture.CreateContext();

        // Act
        int count = await new BookingRepository(context).CountComingPublicBookingsAsync("anna@gmail.com", today, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, count);
    }

    #endregion

    #region Helpers

    private static Guest NewGuest(string email = "anna@example.com") =>
        Guest.Create("Anna", "Jensen", email, "+4512345678", "Denmark", Passport);

    // A request of one guest for the stay, made at a time, with the hash of a token.
    private static BookingRequest Request(string email, string token, DateTimeOffset madeAt)
    {
        Guest guest = NewGuest(email);
        return BookingRequest.Create(
            guest,
            Stay,
            RoomSize.Double,
            1,
            BookingRequestTestData.HashOf(token),
            BookingRequestTestData.KeyOf(guest.Email),
            madeAt);
    }

    private async Task AddAsync(BookingRequest request)
    {
        await using HotelDbContext context = _fixture.CreateContext();
        new BookingRequestRepository(context).Add(request);
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task AddMailsAsync(params SentMail[] mails)
    {
        await using HotelDbContext context = _fixture.CreateContext();
        SentMailRepository repository = new(context);
        foreach (SentMail mail in mails)
        {
            repository.Add(mail);
        }

        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<BookingRequest?> ClaimAsync(string token, DateTimeOffset now)
    {
        await using HotelDbContext context = _fixture.CreateContext();
        return await new BookingRequestRepository(context)
            .ClaimOpenAsync(BookingRequestTestData.HashOf(token), now, TestContext.Current.CancellationToken);
    }

    // PostgreSQL shows a session that waits for an advisory lock as a lock of that kind that is not granted. Each query is
    // a round trip to the database, so the loop asks again without sleeping; it gives up after 1,000 queries.
    private async Task<bool> SomebodyWaitsForTheLockAsync()
    {
        for (int asked = 0; asked < 1000; asked++)
        {
            if (await ScalarAsync("SELECT count(*)::text FROM pg_locks WHERE locktype = 'advisory' AND NOT granted") != "0")
            {
                return true;
            }
        }

        return false;
    }

    private async Task<string> ScalarAsync(string sql)
    {
        await using NpgsqlConnection connection = new(_fixture.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using NpgsqlCommand command = new(sql, connection);
        return Convert.ToString(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }

    // A room of its own, a guest with the email and a coming public booking of two nights from the start.
    private async Task SeedBookingAsync(string email, DateOnly start)
    {
        await using HotelDbContext context = _fixture.CreateContext();
        Room room = Room.Create($"{++_rooms + 200}", 1, RoomSize.Double, 2);
        Guest guest = Guest.Create("Anna", "Jensen", email, "+4512345678", "Denmark", null);
        new RoomRepository(context).Add(room);
        new GuestRepository(context).Add(guest);
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);

        Booking booking = Booking.Create(new DateRange(start, start.AddDays(2)), room.Id, guest.Id, start.AddDays(-10));
        booking.MarkMadeOnPublicSite();
        new BookingRepository(context).Add(booking);
        await new UnitOfWork(context).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    #endregion
}
