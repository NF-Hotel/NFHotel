using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

using NSubstitute;

namespace NFHotel.Application.Tests.Services;

/// <summary>
/// The guest sign-in of MIL-018 with the reference of MIL-085 (ADR-0047, ADR-0049, UC-010): the reference and the email
/// of a booking give a guest session for that booking; every other case gives the one refusal; every try is counted
/// against the reference and the caller address before the booking is read; and the public booking answers with the
/// receipt and a session.
/// </summary>
public sealed class GuestAccessServiceTests
{
    #region Fields

    private const string BookedEmail = "anna@example.com";

    #endregion Fields

    #region Sign-in

    /// <summary>Positive: the right reference and email give a session for exactly that booking (ADR-0049 decision 2).</summary>
    [Fact]
    public async Task SignInAsync_RightReferenceAndEmail_GivesASessionForTheBooking()
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 3);

        // Act
        Result<GuestSessionDto> result = await world.GuestAccessService()
            .SignInAsync(new GuestSignInRequest(BookingReference.Format(booking.Reference), BookedEmail), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(world.GuestTokens.ExpiresAt, result.Value.ExpiresAt);
        Assert.Equal([booking.Id], world.GuestTokens.Issued);
    }

    /// <summary>
    /// Equivalence: the reference is read in any letter case and with spaces around it, and the email is compared without
    /// case and without the spaces around it (ADR-0049 decision 1, MIL-085 criterion 4).
    /// </summary>
    /// <param name="upperCaseReference">Whether the reference is typed in upper case with spaces around it.</param>
    /// <param name="email">The email as the guest types it.</param>
    [Theory]
    [InlineData(true, BookedEmail)]
    [InlineData(false, "ANNA@EXAMPLE.COM")]
    [InlineData(false, "  Anna@Example.com ")]
    public async Task SignInAsync_ReferenceOrEmailInAnotherCaseOrWithSpaces_GivesASession(bool upperCaseReference, string email)
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);
        string reference = BookingReference.Format(booking.Reference);
        if (upperCaseReference)
        {
            reference = $"  {reference.ToUpperInvariant()} ";
        }

        // Act
        Result<GuestSessionDto> result = await world.GuestAccessService()
            .SignInAsync(new GuestSignInRequest(reference, email), TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal([booking.Id], world.GuestTokens.Issued);
    }

    /// <summary>
    /// Security: a wrong email, an unknown reference, an unreadable one, a booking number of the old kind, the key of the
    /// booking and a cancelled booking give the same one refusal, and no token is issued (ADR-0049 decisions 2 and 5,
    /// MIL-085 criterion 4).
    /// </summary>
    /// <param name="refusal">Which kind is tried.</param>
    [Theory]
    [InlineData("wrong email")]
    [InlineData("unknown reference")]
    [InlineData("unreadable reference")]
    [InlineData("old booking number")]
    [InlineData("the key of the booking")]
    [InlineData("cancelled booking")]
    public async Task SignInAsync_EveryKindOfRefusal_GivesTheOneRefusal(string refusal)
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 3);
        if (refusal == "cancelled booking")
        {
            booking.Cancel();
        }

        string reference = BookingReference.Format(booking.Reference);
        GuestSignInRequest request = refusal switch
        {
            "wrong email" => new GuestSignInRequest(reference, "bo@example.com"),
            "unknown reference" => new GuestSignInRequest(BookingReference.Format(Guid.NewGuid()), BookedEmail),
            "unreadable reference" => new GuestSignInRequest("not a reference", BookedEmail),
            "old booking number" => new GuestSignInRequest($"FLZ-{booking.Id:D6}", BookedEmail),
            "the key of the booking" => new GuestSignInRequest(booking.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), BookedEmail),
            _ => new GuestSignInRequest(reference, BookedEmail),
        };

        // Act
        Result<GuestSessionDto> result = await world.GuestAccessService().SignInAsync(request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([ErrorCodes.Api.GuestUnauthorized], result.Errors.Select(error => error.Code));
        Assert.Empty(world.GuestTokens.Issued);
    }

    /// <summary>
    /// Security: a reference that no booking has is read from the database like one that exists, so the cost of the
    /// answer does not tell whether the reference exists (ADR-0049 decision 2).
    /// </summary>
    [Fact]
    public async Task SignInAsync_UnknownReference_IsReadLikeAKnownOne()
    {
        // Arrange
        ServiceWorld world = new();
        Guid unknown = Guid.NewGuid();

        // Act
        Result<GuestSessionDto> result = await world.GuestAccessService()
            .SignInAsync(new GuestSignInRequest(BookingReference.Format(unknown), BookedEmail), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.GuestUnauthorized, result.FirstError?.Code);
        await world.BookingQueries.Received(1).GetReadModelByReferenceAsync(unknown, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Security: every try is counted against the reference and the address of the caller before the booking is read
    /// (ADR-0049 decision 6).
    /// </summary>
    [Fact]
    public async Task SignInAsync_WrongEmail_IsCountedAgainstTheReferenceAndTheAddress()
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);

        // Act
        await world.GuestAccessService()
            .SignInAsync(new GuestSignInRequest(BookingReference.Format(booking.Reference), "bo@example.com"), TestContext.Current.CancellationToken);

        // Assert: the null-forgiving operator fills an argument that DidNotReceiveWithAnyArgs ignores.
        world.GuestSignInLimits.Received(1).TryCount(booking.Reference, FakeCallerAddress.DefaultAddress);
        world.GuestSignInLimits.DidNotReceiveWithAnyArgs().Succeeded(default, default!);
    }

    /// <summary>
    /// Security: a try over a limit is refused with its own code before anything is read, even with the right reference
    /// and email (ADR-0049 decision 6, MIL-085 criterion 4).
    /// </summary>
    [Fact]
    public async Task SignInAsync_OverALimit_IsRefusedWithTooManyLookupsWithoutReading()
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);
        world.GuestSignInLimits.TryCount(booking.Reference, FakeCallerAddress.DefaultAddress).Returns(false);

        // Act
        Result<GuestSessionDto> result = await world.GuestAccessService()
            .SignInAsync(new GuestSignInRequest(BookingReference.Format(booking.Reference), BookedEmail), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([ErrorCodes.Booking.TooManyLookups], result.Errors.Select(error => error.Code));
        await world.BookingQueries.DidNotReceiveWithAnyArgs().GetReadModelByReferenceAsync(default, TestContext.Current.CancellationToken);
        Assert.Empty(world.GuestTokens.Issued);
    }

    /// <summary>
    /// Security: an unreadable reference, an old booking number among them, is counted against the address alone, so one
    /// caller cannot try without a count (ADR-0049 decision 6).
    /// </summary>
    /// <param name="reference">An unreadable reference.</param>
    [Theory]
    [InlineData("FLZ-000123")]
    [InlineData("123")]
    [InlineData("")]
    [InlineData(null)]
    public async Task SignInAsync_UnreadableReference_IsCountedAgainstTheAddressAlone(string? reference)
    {
        // Arrange
        ServiceWorld world = new();

        // Act
        Result<GuestSessionDto> result = await world.GuestAccessService()
            .SignInAsync(new GuestSignInRequest(reference, BookedEmail), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(ErrorCodes.Api.GuestUnauthorized, result.FirstError?.Code);
        world.GuestSignInLimits.Received(1).TryCount(null, FakeCallerAddress.DefaultAddress);
        await world.BookingQueries.DidNotReceiveWithAnyArgs().GetReadModelByReferenceAsync(default, TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// Security: a success clears the count of the reference and gives the address its try back (ADR-0049 decision 6).
    /// </summary>
    [Fact]
    public async Task SignInAsync_Success_ClearsTheReferenceAndGivesTheAddressItsTryBack()
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);

        // Act
        await world.GuestAccessService()
            .SignInAsync(new GuestSignInRequest(BookingReference.Format(booking.Reference), BookedEmail), TestContext.Current.CancellationToken);

        // Assert
        world.GuestSignInLimits.Received(1).TryCount(booking.Reference, FakeCallerAddress.DefaultAddress);
        world.GuestSignInLimits.Received(1).Succeeded(booking.Reference, FakeCallerAddress.DefaultAddress);
    }

    /// <summary>
    /// Security: the right email of a cancelled booking is refused and keeps the count, as a wrong email does, so the
    /// count cannot tell a guesser that the email was right (ADR-0047 decision 7, ADR-0049 decision 6).
    /// </summary>
    [Fact]
    public async Task SignInAsync_RightEmailOfACancelledBooking_IsRefusedAndKeepsTheCount()
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id, startOffsetDays: 3);
        booking.Cancel();

        // Act
        Result<GuestSessionDto> result = await world.GuestAccessService()
            .SignInAsync(new GuestSignInRequest(BookingReference.Format(booking.Reference), BookedEmail), TestContext.Current.CancellationToken);

        // Assert: the null-forgiving operator fills an argument that DidNotReceiveWithAnyArgs ignores.
        Assert.Equal(ErrorCodes.Api.GuestUnauthorized, result.FirstError?.Code);
        world.GuestSignInLimits.Received(1).TryCount(booking.Reference, FakeCallerAddress.DefaultAddress);
        world.GuestSignInLimits.DidNotReceiveWithAnyArgs().Succeeded(default, default!);
        Assert.Empty(world.GuestTokens.Issued);
    }

    /// <summary>
    /// Equivalence (ADR-0049 decision 6, review RC-418): one caller is one key. An IPv4 address in IPv6 form counts as the
    /// IPv4 address, and an IPv6 address counts by its first 64 bits, the network that the caller was given, so a caller
    /// cannot get new tries by changing the last 64 bits, which every host may choose.
    /// </summary>
    /// <param name="address">The address of the caller.</param>
    /// <param name="key">The key that the limits count.</param>
    [Theory]
    [InlineData("203.0.113.7", "203.0.113.7")]
    [InlineData("::ffff:203.0.113.7", "203.0.113.7")]
    [InlineData("2001:db8:1:2::1", "2001:db8:1:2::/64")]
    [InlineData("2001:DB8:1:2:AAAA:BBBB:CCCC:DDDD", "2001:db8:1:2::/64")]
    [InlineData("2001:db8:1:3::1", "2001:db8:1:3::/64")]
    [InlineData("not an address", GuestAccessService.UnknownAddress)]
    public async Task SignInAsync_CallerAddressInAnyForm_IsCountedUnderTheKeyOfItsCaller(string address, string key)
    {
        // Arrange
        ServiceWorld world = new();
        world.CallerAddress.Address = address;
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);

        // Act
        await world.GuestAccessService()
            .SignInAsync(new GuestSignInRequest(BookingReference.Format(booking.Reference), "wrong@example.com"), TestContext.Current.CancellationToken);

        // Assert
        world.GuestSignInLimits.Received(1).TryCount(booking.Reference, key);
    }

    /// <summary>Boundary: a caller whose address the API does not know is counted under one name for all of them.</summary>
    [Fact]
    public async Task SignInAsync_UnknownCallerAddress_IsCountedAsUnknown()
    {
        // Arrange
        ServiceWorld world = new();
        world.CallerAddress.Address = null;
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);

        // Act
        await world.GuestAccessService()
            .SignInAsync(new GuestSignInRequest(BookingReference.Format(booking.Reference), "bo@example.com"), TestContext.Current.CancellationToken);

        // Assert
        world.GuestSignInLimits.Received(1).TryCount(booking.Reference, GuestAccessService.UnknownAddress);
    }

    /// <summary>
    /// Race condition: tries of one reference sent at the same time are each counted before any of them reads, so only
    /// as many reach the read as the limit allows and the rest are refused at once (MIL-018). The reads are held until
    /// every try has started: the interleaving that would let all of them pass a check made apart from the count.
    /// </summary>
    [Fact]
    public async Task SignInAsync_ManyAtTheSameTime_ReadsOnlyAsManyAsTheLimitAllows()
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);
        TaskCompletionSource<BookingReadModel?> heldRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        world.BookingQueries.GetReadModelByReferenceAsync(booking.Reference, Arg.Any<CancellationToken>()).Returns(_ => heldRead.Task);
        GuestAccessService service = world.GuestAccessService(new CountingGuestSignInLimits(maxPerReference: 5, maxPerAddress: 20));
        string reference = BookingReference.Format(booking.Reference);

        // Act
        Task<Result<GuestSessionDto>>[] tries = Enumerable.Range(0, 10)
            .Select(attempt => service.SignInAsync(new GuestSignInRequest(reference, $"guess{attempt}@example.com"), TestContext.Current.CancellationToken))
            .ToArray();
        heldRead.SetResult(null);
        Result<GuestSessionDto>[] results = await Task.WhenAll(tries);

        // Assert
        Assert.Equal(5, results.Count(result => result.FirstError?.Code == ErrorCodes.Api.GuestUnauthorized));
        Assert.Equal(5, results.Count(result => result.FirstError?.Code == ErrorCodes.Booking.TooManyLookups));
        await world.BookingQueries.Received(5).GetReadModelByReferenceAsync(booking.Reference, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Security: one address that tries an email against many references is stopped at the limit of the address, while
    /// another address still gets through (ADR-0049 decision 6).
    /// </summary>
    [Fact]
    public async Task SignInAsync_ManyReferencesFromOneAddress_StopsAtTheLimitOfTheAddress()
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);
        GuestAccessService service = world.GuestAccessService(new CountingGuestSignInLimits(maxPerReference: 5, maxPerAddress: 20));

        // Act
        List<Result<GuestSessionDto>> guesses = [];
        for (int attempt = 0; attempt < 21; attempt++)
        {
            guesses.Add(await service.SignInAsync(new GuestSignInRequest(BookingReference.Format(Guid.NewGuid()), BookedEmail), TestContext.Current.CancellationToken));
        }

        world.CallerAddress.Address = "198.51.100.7";
        Result<GuestSessionDto> fromAnotherAddress = await service
            .SignInAsync(new GuestSignInRequest(BookingReference.Format(booking.Reference), BookedEmail), TestContext.Current.CancellationToken);

        // Assert
        Assert.All(guesses.Take(20), guess => Assert.Equal(ErrorCodes.Api.GuestUnauthorized, guess.FirstError?.Code));
        Assert.Equal(ErrorCodes.Booking.TooManyLookups, guesses[20].FirstError?.Code);
        Assert.True(fromAnotherAddress.IsSuccess);
    }

    /// <summary>Negative: a missing email keeps its own code, which the API answers with 422, counts nothing and issues nothing.</summary>
    /// <param name="email">A missing or blank email.</param>
    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task SignInAsync_NoEmail_KeepsItsCodeAndCountsNothing(string? email)
    {
        // Arrange
        ServiceWorld world = new();
        Booking booking = world.AddBooking(world.AddRoom().Id, world.AddGuest().Id);

        // Act
        Result<GuestSessionDto> result = await world.GuestAccessService()
            .SignInAsync(new GuestSignInRequest(BookingReference.Format(booking.Reference), email), TestContext.Current.CancellationToken);

        // Assert: the null-forgiving operator fills an argument that DidNotReceiveWithAnyArgs ignores.
        Assert.Equal(ErrorCodes.Booking.ReceiptEmailRequired, result.FirstError?.Code);
        world.GuestSignInLimits.DidNotReceiveWithAnyArgs().TryCount(default, default!);
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

    /// <summary>
    /// Positive: the public booking answers with the receipt of the new booking, its reference among it, and a session
    /// for it, and nothing that names the key of the booking (ADR-0049 decisions 3 and 7).
    /// </summary>
    [Fact]
    public async Task BookAsync_FreeRoomOfTheType_AnswersWithTheReceiptAndItsSession()
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
        Booking stored = Assert.Single(world.Bookings.Items);
        Assert.Equal(
            new BookingReceiptDto(stored.Reference, world.Clock.Today.AddDays(4), world.Clock.Today.AddDays(6), 2, RoomSize.Double, BookingStatus.Pending),
            result.Value.Receipt);
        Assert.Equal([stored.Id], world.GuestTokens.Issued);
        Assert.Equal(world.GuestTokens.ExpiresAt, result.Value.Session.ExpiresAt);
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
        IPublicBookingService bookings = world.BookingService();

        // Act and assert: the null-forgiving operator passes the null that the guard must refuse.
        Assert.Throws<ArgumentNullException>(() => new GuestAccessService(null!, world.BookingQueries, world.GuestTokens, world.GuestSignInLimits, world.CallerAddress));
        Assert.Throws<ArgumentNullException>(() => new GuestAccessService(bookings, null!, world.GuestTokens, world.GuestSignInLimits, world.CallerAddress));
        Assert.Throws<ArgumentNullException>(() => new GuestAccessService(bookings, world.BookingQueries, null!, world.GuestSignInLimits, world.CallerAddress));
        Assert.Throws<ArgumentNullException>(() => new GuestAccessService(bookings, world.BookingQueries, world.GuestTokens, null!, world.CallerAddress));
        Assert.Throws<ArgumentNullException>(() => new GuestAccessService(bookings, world.BookingQueries, world.GuestTokens, world.GuestSignInLimits, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => world.GuestAccessService().BookAsync(null!, TestContext.Current.CancellationToken));
    }

    #endregion
}
