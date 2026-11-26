using System.Globalization;

using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;
using NFHotel.TestSupport;

namespace NFHotel.Application.Tests.Services;

/// <summary>
/// The booking request of the public site and its verification by email (UC-011, MIL-088 criteria 3 to 6 and 8, ADR-0052
/// and ADR-0053): a request holds no room and sends one mail with a single-use link of 30 minutes; the button books in one
/// save, signs the guest in and mails the reference; the limits per mailbox, the fuse and a relay that is down.
/// </summary>
public sealed class BookingRequestServiceTests
{
    private const string Email = "anna@example.com";

    #region The setting

    /// <summary>Positive: the mode follows the setting.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetModeAsync_Setting_SaysWhetherTheGuestVerifies(bool enabled)
    {
        // Arrange
        ServiceWorld world = World();
        world.Verification.Enabled = enabled;

        // Act
        Result<PublicBookingModeDto> mode = await world.BookingRequestService().GetModeAsync(Token);

        // Assert
        Assert.Equal(enabled, mode.Value.VerificationRequired);
    }

    /// <summary>Negative: with the setting off, a request is not found and sends nothing (decision 8).</summary>
    [Fact]
    public async Task AskAsync_SettingOff_IsNotFoundAndSendsNothing()
    {
        // Arrange
        ServiceWorld world = World();
        world.Verification.Enabled = false;

        // Act
        Result<BookingRequestReceiptDto> result = await world.BookingRequestService().AskAsync(Request(world), Token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.NotFound], Codes(result));
        Assert.Empty(world.BookingRequests.Items);
        Assert.Empty(world.Mails.Attempts);
    }

    /// <summary>Negative: with the setting off, a verification is not found.</summary>
    [Fact]
    public async Task VerifyAsync_SettingOff_IsNotFound()
    {
        // Arrange
        ServiceWorld world = World();
        string token = await AskAsync(world);
        world.Verification.Enabled = false;

        // Act
        Result<PublicBookingDto> result = await VerifyAsync(world, token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.NotFound], Codes(result));
        Assert.Empty(world.Bookings.Items);
    }

    /// <summary>Negative: with the setting on, the public booking at once is refused, so no caller books around the verification.</summary>
    [Fact]
    public async Task CreatePublicAsync_SettingOn_IsRefusedAndStoresNothing()
    {
        // Arrange
        ServiceWorld world = World();

        // Act
        Result<BookingDetailsDto> result = await world.BookingService().CreatePublicAsync(Request(world), Token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.VerificationRequired], Codes(result));
        Assert.Empty(world.Bookings.Items);
        Assert.Empty(world.Guests.Items);
    }

    #endregion

    #region Ask

    /// <summary>
    /// Positive (criterion 3): a request is stored with the time its link lapses, stores no guest and no booking, and sends
    /// one mail to the email.
    /// </summary>
    [Fact]
    public async Task AskAsync_ValidRequest_StoresARequestAndSendsOneMail()
    {
        // Arrange
        ServiceWorld world = World();

        // Act
        Result<BookingRequestReceiptDto> result = await world.BookingRequestService().AskAsync(Request(world), Token);

        // Assert
        Assert.Equal(world.Clock.UtcNow.AddMinutes(30), result.Value.ExpiresAt);
        Assert.Single(world.BookingRequests.Items);
        Assert.Empty(world.Bookings.Items);
        Assert.Empty(world.Guests.Items);
        OutgoingMail mail = Assert.Single(world.Mails.Sent);
        Assert.Equal((Email, "Verify your booking at NF Hotel"), (mail.To, mail.Subject));
        Assert.Equal(["mail_sent VerificationLink", "request_made"], world.RequestEvents.Lines);
    }

    /// <summary>Positive (criterion 3): a request holds no room, so the next guest still gets the same last room.</summary>
    [Fact]
    public async Task AskAsync_LastRoomAskedFor_IsStillOfferedToTheNextGuest()
    {
        // Arrange
        ServiceWorld world = World(doubleRooms: 1);
        string first = await AskAsync(world, "first@example.com");

        // Act
        string second = await AskAsync(world, "second@example.com");
        Result<PublicBookingDto> secondBooks = await VerifyAsync(world, second);
        Result<PublicBookingDto> firstTooLate = await VerifyAsync(world, first);

        // Assert
        Assert.True(secondBooks.IsSuccess);
        Assert.Equal([ErrorCodes.Booking.RoomTypeNotAvailable], Codes(firstTooLate));
    }

    /// <summary>
    /// Positive (criterion 3): the mail holds the dates, the room size, the number of guests, the link and its time, and
    /// no text that was typed into the form, not even the email.
    /// </summary>
    [Fact]
    public async Task AskAsync_Mail_HoldsTheStayAndTheLinkAndNoTypedText()
    {
        // Arrange
        ServiceWorld world = World();
        CreatePublicBookingRequest request = Request(world);

        // Act
        await world.BookingRequestService().AskAsync(request, Token);

        // Assert
        string body = Assert.Single(world.Mails.Sent).Body;
        Assert.Contains($"Arrival: {Long(request.CheckInDate)}", body, StringComparison.Ordinal);
        Assert.Contains($"Departure: {Long(request.CheckOutDate)} (2 nights)", body, StringComparison.Ordinal);
        Assert.Contains("Room: Double room", body, StringComparison.Ordinal);
        Assert.Contains("Guests: 1", body, StringComparison.Ordinal);
        Assert.Contains($"https://hotel.example.com/booking/verify#{world.Mails.LastToken()}", body, StringComparison.Ordinal);
        Assert.Contains("until 12:30 UTC on Thursday 1 January 2026", body, StringComparison.Ordinal);
        string withoutToken = body.Replace(world.Mails.LastToken(), string.Empty, StringComparison.Ordinal); // a random token may spell a word
        Assert.All(["Anna", "Jensen", "+4512345678", "Denmark", Email], typed => Assert.DoesNotContain(typed, withoutToken, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Positive (decision 2): the link is built from the setting of the site address, also below a path.</summary>
    [Fact]
    public async Task AskAsync_SiteAddressWithAPath_BuildsTheLinkBelowIt()
    {
        // Arrange
        ServiceWorld world = World();
        world.Verification.SiteAddress = new Uri("https://www.example.com/hotel");

        // Act
        await world.BookingRequestService().AskAsync(Request(world), Token);

        // Assert
        Assert.Contains($"https://www.example.com/hotel/booking/verify#{world.Mails.LastToken()}", world.Mails.Sent[0].Body, StringComparison.Ordinal);
    }

    /// <summary>Positive (decision 2): the token is 43 characters of base64url, and only its SHA-256 is stored.</summary>
    [Fact]
    public async Task AskAsync_Token_IsStoredOnlyAsItsHash()
    {
        // Arrange
        ServiceWorld world = World();

        // Act
        string token = await AskAsync(world);

        // Assert
        Assert.Matches("^[A-Za-z0-9_-]{43}$", token);
        BookingRequest stored = Assert.Single(world.BookingRequests.Items);
        Assert.Equal(BookingRequestTestData.HashOf(token), stored.TokenHash);
        Assert.Equal(BookingRequestTestData.KeyOf(Email), stored.EmailKey);
    }

    /// <summary>Positive (decision 2): two requests get two different tokens.</summary>
    [Fact]
    public async Task AskAsync_TwoRequests_GetDifferentTokens()
    {
        // Arrange
        ServiceWorld world = World();

        // Act
        string first = await AskAsync(world, "first@example.com");
        string second = await AskAsync(world, "second@example.com");

        // Assert
        Assert.NotEqual(first, second);
    }

    /// <summary>Negative: a rule of the public booking refuses the request, and nothing is stored or sent.</summary>
    [Fact]
    public async Task AskAsync_PartyTooLarge_IsRefusedAndSendsNothing()
    {
        // Arrange
        ServiceWorld world = World();

        // Act
        Result<BookingRequestReceiptDto> result = await world.BookingRequestService().AskAsync(Request(world, guests: 5), Token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.GuestsOutOfRange], Codes(result));
        Assert.Empty(world.BookingRequests.Items);
        Assert.Empty(world.Mails.Attempts);
    }

    /// <summary>Negative: no free room of the size, so no request and no mail.</summary>
    [Fact]
    public async Task AskAsync_NoFreeRoomOfTheSize_IsRefusedAndSendsNothing()
    {
        // Arrange
        ServiceWorld world = World();
        CreatePublicBookingRequest suite = Request(world) with { RoomSize = RoomSize.Suite };

        // Act
        Result<BookingRequestReceiptDto> result = await world.BookingRequestService().AskAsync(suite, Token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.RoomTypeNotAvailable], Codes(result));
        Assert.Empty(world.Mails.Attempts);
    }

    /// <summary>Negative (criterion 6): a line break in the email is refused by the rule of the email before any mail is built.</summary>
    [Fact]
    public async Task AskAsync_LineBreakInTheEmail_IsRefusedBeforeAnyMail()
    {
        // Arrange
        ServiceWorld world = World();

        // Act
        Result<BookingRequestReceiptDto> result = await world.BookingRequestService().AskAsync(Request(world, "anna@example.com\r\nBcc: eve@example.com"), Token);

        // Assert
        Assert.Contains(ErrorCodes.Guest.EmailInvalid, Codes(result));
        Assert.Empty(world.Mails.Attempts);
        Assert.Empty(world.BookingRequests.Items);
    }

    /// <summary>
    /// Negative (decision 6): a mailbox with three coming public bookings asks for a fourth in another form of its email,
    /// and is refused by the limit per mailbox with its event, which holds no email.
    /// </summary>
    [Fact]
    public async Task AskAsync_FourthComingBookingOfTheMailbox_IsRefused()
    {
        // Arrange
        ServiceWorld world = World(doubleRooms: 4);
        world.Verification.Enabled = false;
        for (int stay = 0; stay < 3; stay++)
        {
            Assert.True((await world.BookingService().CreatePublicAsync(Request(world, "anna@gmail.com", stay), Token)).IsSuccess);
        }

        world.Verification.Enabled = true;

        // Act
        Result<BookingRequestReceiptDto> result = await world.BookingRequestService().AskAsync(Request(world, "A.nna+4@GoogleMail.com", 3), Token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.TooManyRequests], Codes(result));
        Assert.Equal([$"limit_refused {LimitNames.Mailbox}"], world.RequestEvents.Lines);
        Assert.Empty(world.Mails.Attempts);
    }

    #endregion

    #region One open request per mailbox

    /// <summary>
    /// State transition (criterion 5): a new request of the same mailbox, in another form of its email, replaces the old
    /// one, whose link stops working; the new link books.
    /// </summary>
    [Fact]
    public async Task AskAsync_SecondRequestOfTheMailbox_MakesTheFirstLinkUseless()
    {
        // Arrange
        ServiceWorld world = World();
        string first = await AskAsync(world, "anna@gmail.com");

        // Act
        string second = await AskAsync(world, "a.nna+trip@googlemail.com", stay: 1);
        Result<PublicBookingDto> firstLink = await VerifyAsync(world, first);
        Result<PublicBookingDto> secondLink = await VerifyAsync(world, second);

        // Assert
        Assert.Equal([ErrorCodes.Booking.VerificationLinkInvalid], Codes(firstLink));
        Assert.True(secondLink.IsSuccess);
        Assert.Equal(Request(world, stay: 1).CheckInDate, secondLink.Value.Receipt.StartDate);
    }

    /// <summary>
    /// Boundary (criterion 5): the fourth verification mail to one mailbox within an hour is refused, the third request
    /// stays and its link still books.
    /// </summary>
    [Fact]
    public async Task AskAsync_FourthMailInAnHour_IsRefusedAndKeepsTheThirdRequest()
    {
        // Arrange
        ServiceWorld world = World();
        await AskAsync(world);
        world.Clock.TimeOfDay += TimeSpan.FromMinutes(20);
        await AskAsync(world);
        world.Clock.TimeOfDay += TimeSpan.FromMinutes(20);
        string third = await AskAsync(world);
        world.Clock.TimeOfDay += TimeSpan.FromMinutes(19);

        // Act
        Result<BookingRequestReceiptDto> fourth = await world.BookingRequestService().AskAsync(Request(world, "Anna+again@Example.com"), Token);
        Result<PublicBookingDto> thirdLink = await VerifyAsync(world, third);

        // Assert
        Assert.Equal([ErrorCodes.Booking.TooManyMails], Codes(fourth));
        Assert.Contains($"limit_refused {LimitNames.VerificationMail}", world.RequestEvents.Lines);
        Assert.Equal(3, world.Mails.Sent.Count(mail => mail.Subject.StartsWith("Verify", StringComparison.Ordinal)));
        Assert.True(thirdLink.IsSuccess);
    }

    /// <summary>Boundary: a mail sent more than an hour ago no longer counts in the hour.</summary>
    [Fact]
    public async Task AskAsync_FourthMailAfterTheHour_IsSent()
    {
        // Arrange
        ServiceWorld world = World();
        for (int mail = 0; mail < 3; mail++)
        {
            await AskAsync(world);
        }

        world.Clock.TimeOfDay += TimeSpan.FromMinutes(61);

        // Act
        Result<BookingRequestReceiptDto> fourth = await world.BookingRequestService().AskAsync(Request(world), Token);

        // Assert
        Assert.True(fourth.IsSuccess);
    }

    /// <summary>Boundary (criterion 5): the sixth verification mail to one mailbox within a day is refused.</summary>
    [Fact]
    public async Task AskAsync_SixthMailInADay_IsRefused()
    {
        // Arrange
        ServiceWorld world = World();
        world.Clock.TimeOfDay = TimeSpan.FromHours(6);
        for (int mail = 0; mail < 5; mail++)
        {
            await AskAsync(world);
            world.Clock.TimeOfDay += TimeSpan.FromHours(2);
        }

        // Act
        Result<BookingRequestReceiptDto> sixth = await world.BookingRequestService().AskAsync(Request(world), Token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.TooManyMails], Codes(sixth));
    }

    /// <summary>Positive: the limits per mailbox count the requests under the one lock of the database (decision 6).</summary>
    [Fact]
    public async Task AskAsync_Request_CountsAndWritesUnderTheLock()
    {
        // Arrange
        ServiceWorld world = World();

        // Act
        await AskAsync(world);

        // Assert
        Assert.Equal(["booking_request"], world.UnitOfWork.Locks);
    }

    #endregion

    #region The fuse

    /// <summary>
    /// Boundary (ADR-0053 decision 6): at the fuse a new request is refused with its code and event, and no mail is sent;
    /// the fuse counts the mails of every mailbox and kind.
    /// </summary>
    [Fact]
    public async Task AskAsync_FuseReached_IsPausedAndSendsNothing()
    {
        // Arrange
        ServiceWorld world = World();
        world.Verification.MaxMailsPerDay = 3;
        world.SentMails.AddSent("one@example.com", MailKind.VerificationLink, world.Clock.UtcNow.AddHours(-23));
        world.SentMails.AddSent("two@example.com", MailKind.BookingReceipt, world.Clock.UtcNow.AddHours(-2));
        world.SentMails.AddSent("three@example.com", MailKind.VerificationLink, world.Clock.UtcNow.AddMinutes(-1));

        // Act
        Result<BookingRequestReceiptDto> result = await world.BookingRequestService().AskAsync(Request(world), Token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.MailPaused], Codes(result));
        Assert.Equal(["mail_fuse_blown"], world.RequestEvents.Lines);
        Assert.Empty(world.Mails.Attempts);
        Assert.Empty(world.BookingRequests.Items);
    }

    /// <summary>Boundary: a mail sent more than 24 hours ago no longer counts against the fuse.</summary>
    [Fact]
    public async Task AskAsync_FuseWindowPassed_IsSent()
    {
        // Arrange
        ServiceWorld world = World();
        world.Verification.MaxMailsPerDay = 1;
        world.SentMails.AddSent("one@example.com", MailKind.VerificationLink, world.Clock.UtcNow.AddHours(-24));

        // Act
        Result<BookingRequestReceiptDto> result = await world.BookingRequestService().AskAsync(Request(world), Token);

        // Assert
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region The relay

    /// <summary>
    /// Negative (criterion 6): the relay does not take the mail; the guest is told, no request and no record are left, and
    /// the event names the kind and the reason.
    /// </summary>
    [Fact]
    public async Task AskAsync_RelayDown_TellsTheGuestAndLeavesNothing()
    {
        // Arrange
        ServiceWorld world = World();
        world.Mails.RefuseAll(MailFailureReasons.Connect);

        // Act
        Result<BookingRequestReceiptDto> result = await world.BookingRequestService().AskAsync(Request(world), Token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.MailNotSent], Codes(result));
        Assert.Empty(world.BookingRequests.Items);
        Assert.Empty(world.SentMails.Items);
        Assert.Equal(["mail_not_sent VerificationLink connect"], world.RequestEvents.Lines);
    }

    /// <summary>Negative: a renewal whose mail fails leaves no request, and the old link does not come back.</summary>
    [Fact]
    public async Task AskAsync_RelayDownForARenewal_LeavesNoRequest()
    {
        // Arrange
        ServiceWorld world = World();
        string first = await AskAsync(world);
        world.Mails.RefuseAll(MailFailureReasons.Timeout);

        // Act
        Result<BookingRequestReceiptDto> renewal = await world.BookingRequestService().AskAsync(Request(world, stay: 1), Token);
        Result<PublicBookingDto> firstLink = await VerifyAsync(world, first);

        // Assert
        Assert.Equal([ErrorCodes.Booking.MailNotSent], Codes(renewal));
        Assert.Empty(world.BookingRequests.Items);
        Assert.Equal([ErrorCodes.Booking.VerificationLinkInvalid], Codes(firstLink));
    }

    #endregion

    #region Verify

    /// <summary>
    /// Positive (criterion 4): the button books a <c>Pending</c> booking of the public site with a reference and its guest,
    /// signs the guest in, removes the request and sends one mail with the reference.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_OpenLink_BooksSignsInAndMailsTheReference()
    {
        // Arrange
        ServiceWorld world = World();
        string token = await AskAsync(world);

        // Act
        Result<PublicBookingDto> result = await VerifyAsync(world, token);

        // Assert
        Booking booking = Assert.Single(world.Bookings.Items);
        Guest guest = Assert.Single(world.Guests.Items);
        Assert.Equal((BookingStatus.Pending, true, guest.Id), (booking.Status, booking.MadeOnPublicSite, booking.GuestId));
        Assert.Equal((booking.Reference, BookingStatus.Pending), (result.Value.Receipt.Reference, result.Value.Receipt.Status));
        Assert.Equal([booking.Id], world.GuestTokens.Issued);
        Assert.Empty(world.BookingRequests.Items);
        OutgoingMail receipt = world.Mails.Sent[^1];
        Assert.Equal((Email, "Your booking at NF Hotel"), (receipt.To, receipt.Subject));
        Assert.Contains($"Reference: {booking.Reference}", receipt.Body, StringComparison.Ordinal);
        Assert.Equal(["mail_sent VerificationLink", "request_made", "public_booking_made", "mail_sent BookingReceipt"], world.RequestEvents.Lines);
    }

    /// <summary>Positive (criterion 4): the guest is stored with the answers of the request.</summary>
    [Fact]
    public async Task VerifyAsync_OpenLink_StoresTheGuestOfTheRequest()
    {
        // Arrange
        ServiceWorld world = World();
        string token = await AskAsync(world, "Anna+trip@Example.com");

        // Act
        await VerifyAsync(world, token);

        // Assert
        Guest guest = Assert.Single(world.Guests.Items);
        Assert.Equal(("Anna", "Jensen", "Anna+trip@Example.com", "Denmark"), (guest.FirstName, guest.LastName, guest.Email, guest.Country));
    }

    /// <summary>Negative (criterion 3): a second use, a wrong token, a malformed one and a lapsed one get the same answer.</summary>
    [Fact]
    public async Task VerifyAsync_UsedWrongMalformedOrLapsedLink_GetsTheSameAnswer()
    {
        // Arrange
        ServiceWorld world = World(doubleRooms: 3);
        string used = await AskAsync(world, "used@example.com");
        await VerifyAsync(world, used);
        string lapsed = await AskAsync(world, "lapsed@example.com", stay: 1);
        world.Clock.TimeOfDay += TimeSpan.FromMinutes(30);
        string wrong = new('A', 43);

        // Act
        Result<PublicBookingDto>[] results =
        [
            await VerifyAsync(world, used),
            await VerifyAsync(world, wrong),
            await VerifyAsync(world, "not a token"),
            await VerifyAsync(world, string.Empty),
            await VerifyAsync(world, lapsed),
        ];

        // Assert
        Assert.All(results, result => Assert.Equal([ErrorCodes.Booking.VerificationLinkInvalid], Codes(result)));
        Assert.Equal(5, world.RequestEvents.Lines.Count(line => line == "verification_failed"));
        Assert.Single(world.Bookings.Items);
    }

    /// <summary>Boundary (criterion 3): the link still books a moment before its 30 minutes are over.</summary>
    [Fact]
    public async Task VerifyAsync_JustBeforeTheLinkLapses_Books()
    {
        // Arrange
        ServiceWorld world = World();
        string token = await AskAsync(world);
        world.Clock.TimeOfDay += TimeSpan.FromMinutes(30) - TimeSpan.FromTicks(1);

        // Act
        Result<PublicBookingDto> result = await VerifyAsync(world, token);

        // Assert
        Assert.True(result.IsSuccess);
    }

    /// <summary>
    /// Negative (criterion 4): the room went between the mail and the button; the guest is told, and no request, no guest
    /// and no session are left.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_RoomWentBeforeTheButton_LeavesNothing()
    {
        // Arrange
        ServiceWorld world = World(doubleRooms: 1);
        string token = await AskAsync(world);
        Guest staffGuest = world.AddGuest();
        world.AddBooking(world.Rooms.Items[0].Id, staffGuest.Id, startOffsetDays: 5, nights: 2);

        // Act
        Result<PublicBookingDto> result = await VerifyAsync(world, token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.RoomTypeNotAvailable], Codes(result));
        Assert.Empty(world.BookingRequests.Items);
        Assert.Equal([staffGuest], world.Guests.Items);
        Assert.Empty(world.GuestTokens.Issued);
    }

    /// <summary>
    /// Negative (decision 5): the save finds the chosen room taken by a booking that committed in between; the booking and
    /// its guest are taken back and the next free room is booked.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_ChosenRoomLostAtTheSave_BooksTheNextFreeRoom()
    {
        // Arrange
        ServiceWorld world = World(doubleRooms: 2);
        string token = await AskAsync(world);
        world.UnitOfWork.FailNextSaves.Enqueue(new BookingOverlapConflictException("another guest booked the room first"));

        // Act
        Result<PublicBookingDto> result = await VerifyAsync(world, token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(world.Bookings.Discarded);
        Assert.Equal(world.Rooms.Items[1].Id, Assert.Single(world.Bookings.Items).RoomId);
        Assert.Single(world.Guests.Items);
    }

    /// <summary>Negative (criterion 4): the last room is lost at the save; no guest is left without a booking.</summary>
    [Fact]
    public async Task VerifyAsync_LastRoomLostAtTheSave_LeavesNoGuest()
    {
        // Arrange
        ServiceWorld world = World(doubleRooms: 1);
        string token = await AskAsync(world);
        world.UnitOfWork.FailNextSaves.Enqueue(new BookingOverlapConflictException("another guest booked the room first"));

        // Act
        Result<PublicBookingDto> result = await VerifyAsync(world, token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.RoomTypeNotAvailable], Codes(result));
        Assert.Empty(world.Bookings.Items);
        Assert.Empty(world.Guests.Items);
        Assert.Empty(world.GuestTokens.Issued);
    }

    /// <summary>Boundary (decision 5): a verification tries five rooms at most.</summary>
    [Fact]
    public async Task VerifyAsync_FiveRoomsLost_StopsAtFive()
    {
        // Arrange
        ServiceWorld world = World(doubleRooms: 7);
        string token = await AskAsync(world);
        for (int lost = 0; lost < 6; lost++)
        {
            world.UnitOfWork.FailNextSaves.Enqueue(new BookingOverlapConflictException("another guest booked the room first"));
        }

        // Act
        Result<PublicBookingDto> result = await VerifyAsync(world, token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.RoomTypeNotAvailable], Codes(result));
        Assert.Equal(5, world.Bookings.Discarded.Count);
        Assert.Empty(world.Guests.Items);
    }

    /// <summary>
    /// Negative (decision 5): the limit of 3 coming public bookings per mailbox is checked again at the verification.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_MailboxReachedTheLimitSinceTheRequest_IsRefused()
    {
        // Arrange
        ServiceWorld world = World(doubleRooms: 4);
        world.Verification.Enabled = false;
        for (int stay = 0; stay < 2; stay++)
        {
            Assert.True((await world.BookingService().CreatePublicAsync(Request(world, stay: stay), Token)).IsSuccess);
        }

        world.Verification.Enabled = true;
        string token = await AskAsync(world, stay: 2);
        Guest sameMailbox = world.AddGuest("Anna", "Jensen", "anna+other@example.com", null);
        Booking third = world.AddBooking(world.Rooms.Items[3].Id, sameMailbox.Id, startOffsetDays: 30, nights: 2);
        third.MarkMadeOnPublicSite();

        // Act
        Result<PublicBookingDto> result = await VerifyAsync(world, token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.TooManyRequests], Codes(result));
        Assert.Equal(3, world.Bookings.Items.Count);
    }

    /// <summary>
    /// Negative (ADR-0053 decision 5): the relay does not take the second mail; the booking stays, and the event names the
    /// receipt.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_ReceiptMailRefused_KeepsTheBooking()
    {
        // Arrange
        ServiceWorld world = World();
        string token = await AskAsync(world);
        world.Mails.Refuse = mail => mail.Subject == "Your booking at NF Hotel" ? MailFailureReasons.Refused : null;

        // Act
        Result<PublicBookingDto> result = await VerifyAsync(world, token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(world.Bookings.Items);
        Assert.Contains("mail_not_sent BookingReceipt refused", world.RequestEvents.Lines);
        Assert.Equal([MailKind.VerificationLink], world.SentMails.Items.Select(mail => mail.Kind));
    }

    /// <summary>Boundary (ADR-0053 decision 6): over the fuse the second mail is not sent, and the booking stays.</summary>
    [Fact]
    public async Task VerifyAsync_FuseReachedBeforeTheReceipt_KeepsTheBookingAndSendsNoReceipt()
    {
        // Arrange
        ServiceWorld world = World();
        world.Verification.MaxMailsPerDay = 2;
        string token = await AskAsync(world);
        world.SentMails.AddSent("other@example.com", MailKind.VerificationLink, world.Clock.UtcNow);

        // Act
        Result<PublicBookingDto> result = await VerifyAsync(world, token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(world.Mails.Sent);
        Assert.Contains("mail_fuse_blown", world.RequestEvents.Lines);
        Assert.Contains($"mail_not_sent BookingReceipt {MailFailureReasons.Fuse}", world.RequestEvents.Lines);
    }

    /// <summary>
    /// Negative (ADR-0052 decision 5, RC-436 F1): the send of the second mail ends with an error that is no answer of the
    /// relay; the booking, the receipt and the session stay, and the event names the receipt.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_SendOfTheReceiptThrows_KeepsTheBookingAndTheReceipt()
    {
        // Arrange
        ServiceWorld world = World();
        string token = await AskAsync(world);
        world.Mails.Throw = mail => mail.Subject == "Your booking at NF Hotel" ? new InvalidOperationException("The relay broke.") : null;

        // Act
        Result<PublicBookingDto> result = await VerifyAsync(world, token);

        // Assert
        Booking booking = Assert.Single(world.Bookings.Items);
        Assert.Equal(booking.Reference, result.Value.Receipt.Reference);
        Assert.Equal([booking.Id], world.GuestTokens.Issued);
        Assert.Equal($"mail_not_sent BookingReceipt {MailFailureReasons.Other}", world.RequestEvents.Lines[^1]);
    }

    /// <summary>
    /// Negative (ADR-0052 decision 5, RC-436 F1): the database refuses the record of the second mail after the retries;
    /// the booking and the receipt stay, no mail goes, and the event names the receipt.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_RecordOfTheReceiptCannotBeWritten_KeepsTheBookingAndTheReceipt()
    {
        // Arrange
        ServiceWorld world = World();
        string token = await AskAsync(world);
        world.UnitOfWork.FailUnderLock = name =>
            name == BookingRequestService.LockName ? new InvalidOperationException("The database could not save.") : null;

        // Act
        Result<PublicBookingDto> result = await VerifyAsync(world, token);

        // Assert
        Booking booking = Assert.Single(world.Bookings.Items);
        Assert.Equal(booking.Reference, result.Value.Receipt.Reference);
        Assert.Single(world.Mails.Attempts);
        Assert.Equal($"mail_not_sent BookingReceipt {MailFailureReasons.Other}", world.RequestEvents.Lines[^1]);
    }

    /// <summary>
    /// Negative (ADR-0052 decision 5, RC-436 F1): the relay refuses the second mail and the database refuses the removal
    /// of its record; the booking and the receipt stay, and the event names the receipt.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_RecordOfARefusedReceiptCannotBeRemoved_KeepsTheBookingAndTheReceipt()
    {
        // Arrange
        ServiceWorld world = World();
        string token = await AskAsync(world);
        world.Mails.Refuse = mail =>
        {
            if (mail.Subject != "Your booking at NF Hotel")
            {
                return null;
            }

            world.UnitOfWork.FailNextSaves.Enqueue(new InvalidOperationException("The database could not save."));
            return MailFailureReasons.Refused;
        };

        // Act
        Result<PublicBookingDto> result = await VerifyAsync(world, token);

        // Assert
        Booking booking = Assert.Single(world.Bookings.Items);
        Assert.Equal(booking.Reference, result.Value.Receipt.Reference);
        Assert.Equal($"mail_not_sent BookingReceipt {MailFailureReasons.Other}", world.RequestEvents.Lines[^1]);
    }

    #endregion

    #region The public booking at once

    /// <summary>
    /// Negative (decision 8): with the setting off, the public booking that loses its last room at the save leaves no guest
    /// behind, because the guest and the booking are stored in one save.
    /// </summary>
    [Fact]
    public async Task CreatePublicAsync_LastRoomLostAtTheSave_LeavesNoGuest()
    {
        // Arrange
        ServiceWorld world = World(doubleRooms: 1);
        world.Verification.Enabled = false;
        world.UnitOfWork.FailNextSaves.Enqueue(new BookingOverlapConflictException("another guest booked the room first"));

        // Act
        Result<BookingDetailsDto> result = await world.BookingService().CreatePublicAsync(Request(world), Token);

        // Assert
        Assert.Equal([ErrorCodes.Booking.RoomTypeNotAvailable], Codes(result));
        Assert.Empty(world.Guests.Items);
        Assert.Empty(world.Bookings.Items);
    }

    /// <summary>Positive (decision 8): with the setting off, a lost race moves on to the next free room too.</summary>
    [Fact]
    public async Task CreatePublicAsync_ChosenRoomLostAtTheSave_BooksTheNextFreeRoom()
    {
        // Arrange
        ServiceWorld world = World(doubleRooms: 2);
        world.Verification.Enabled = false;
        world.UnitOfWork.FailNextSaves.Enqueue(new BookingOverlapConflictException("another guest booked the room first"));

        // Act
        Result<BookingDetailsDto> result = await world.BookingService().CreatePublicAsync(Request(world), Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(world.Rooms.Items[1].Id, result.Value.Room.RoomId);
        Assert.Single(world.Guests.Items);
    }

    /// <summary>
    /// Positive (decision 8, SAR-002 abuse case 6): with the setting off, the public booking counts the bookings of the
    /// mailbox, chooses its room and saves under the one lock of the public bookings, so two guests who book at the same
    /// time take their rooms one after the other.
    /// </summary>
    [Fact]
    public async Task CreatePublicAsync_FreeRoom_ChoosesAndSavesUnderThePublicBookingLock()
    {
        // Arrange
        ServiceWorld world = World();
        world.Verification.Enabled = false;

        // Act
        Result<BookingDetailsDto> result = await world.BookingService().CreatePublicAsync(Request(world), Token);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(world.Bookings.Items);
        Assert.Equal([BookingService.PublicBookingLock], world.UnitOfWork.Locks);
        Assert.True(world.UnitOfWork.SaveCount > 0);
        Assert.Equal(world.UnitOfWork.SaveCount, world.UnitOfWork.SavesUnderLock);
    }

    /// <summary>
    /// Negative (decision 8, SAR-002 abuse case 6): when the lock of the public bookings cannot be taken, no room is
    /// chosen and nothing is saved, so none of that runs before the lock.
    /// </summary>
    [Fact]
    public async Task CreatePublicAsync_LockNotTaken_BooksAndSavesNothing()
    {
        // Arrange
        ServiceWorld world = World();
        world.Verification.Enabled = false;
        world.UnitOfWork.FailUnderLock = name =>
            name == BookingService.PublicBookingLock ? new InvalidOperationException("The database could not take the lock.") : null;

        // Act and assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => world.BookingService().CreatePublicAsync(Request(world), Token));
        Assert.Empty(world.Bookings.Items);
        Assert.Equal(0, world.UnitOfWork.SaveCount);
    }

    #endregion

    #region The clean-up

    /// <summary>
    /// Positive (criterion 5): the clean-up deletes the requests whose link lapsed and the records older than a day, and
    /// keeps the rest.
    /// </summary>
    [Fact]
    public async Task RunAsync_LapsedRequestsAndOldRecords_AreDeleted()
    {
        // Arrange
        ServiceWorld world = World();
        await AskAsync(world, "lapsed@example.com");
        world.Clock.TimeOfDay += TimeSpan.FromMinutes(31);
        await AskAsync(world, "open@example.com", stay: 1);
        world.SentMails.AddSent("old@example.com", MailKind.BookingReceipt, world.Clock.UtcNow.AddHours(-24));

        // Act
        (int requests, int mails) = await world.BookingRequestCleanup().RunAsync(Token);

        // Assert
        Assert.Equal((1, 1), (requests, mails));
        Assert.Equal(BookingRequestTestData.KeyOf("open@example.com"), Assert.Single(world.BookingRequests.Items).EmailKey);
        Assert.Equal(2, world.SentMails.Items.Count);
    }

    #endregion

    #region Helpers

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    // Double rooms that sleep two, with the verification on; the clock stands at 12:00 UTC on 1 January 2026.
    private static ServiceWorld World(int doubleRooms = 2)
    {
        ServiceWorld world = new();
        world.Verification.Enabled = true;
        for (int number = 1; number <= doubleRooms; number++)
        {
            world.AddRoom($"10{number}", 1, RoomSize.Double, 2);
        }

        return world;
    }

    // A stay of two nights of its own for each number, three days after the one before, from five days ahead.
    private static CreatePublicBookingRequest Request(ServiceWorld world, string email = Email, int stay = 0, int guests = 1)
    {
        DateOnly checkIn = world.Clock.Today.AddDays(5 + (3 * stay));
        return new CreatePublicBookingRequest(
            checkIn,
            checkIn.AddDays(2),
            RoomSize.Double,
            guests,
            new GuestFields("Anna", "Jensen", email, "+4512345678", "Denmark", null));
    }

    private static async Task<string> AskAsync(ServiceWorld world, string email = Email, int stay = 0)
    {
        Result<BookingRequestReceiptDto> result = await world.BookingRequestService().AskAsync(Request(world, email, stay), Token);
        Assert.True(result.IsSuccess, string.Join(", ", Codes(result)));
        return world.Mails.LastToken();
    }

    private static Task<Result<PublicBookingDto>> VerifyAsync(ServiceWorld world, string token) =>
        world.BookingRequestService().VerifyAsync(new VerifyBookingRequest(token), Token);

    private static string[] Codes(Result result) => [.. result.Errors.Select(error => error.Code)];

    private static string Long(DateOnly date) => date.ToString("dddd d MMMM yyyy", CultureInfo.InvariantCulture);

    #endregion
}
