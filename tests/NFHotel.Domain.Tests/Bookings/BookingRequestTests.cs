using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Bookings;

/// <summary>
/// The booking request of the public site (MIL-088, ADR-0052): it keeps the checked answers of the guest and the hash of
/// the token, its link works for 30 minutes, and a renewal replaces everything but the key of the mailbox.
/// </summary>
public sealed class BookingRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateRange Stay = new(new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 12));
    private static readonly string FirstHash = new('a', BookingRequest.HashLength);
    private static readonly string SecondHash = new('b', BookingRequest.HashLength);
    private static readonly string MailboxKey = new('c', BookingRequest.HashLength);

    #region Create

    /// <summary>Positive: the request keeps the stay, the size, the party, the fields of the guest and the two hashes.</summary>
    [Fact]
    public void Create_CheckedGuest_KeepsTheAnswers()
    {
        // Act
        BookingRequest request = BookingRequest.Create(NewGuest(), Stay, RoomSize.Double, 2, FirstHash, MailboxKey, Now);

        // Assert
        Assert.Equal(
            ("Anna", "Jensen", "anna@example.com", "+4512345678", "Denmark", "AB123456"),
            (request.FirstName, request.LastName, request.Email, request.PhoneNumber, request.Country, request.PassportNumber));
        Assert.Equal((Stay, RoomSize.Double, 2), (request.Period, request.RoomSize, request.Guests));
        Assert.Equal((FirstHash, MailboxKey), (request.TokenHash, request.EmailKey));
    }

    /// <summary>Boundary: the link works until 30 minutes after the request, and not at that moment.</summary>
    [Fact]
    public void IsOpenAt_ThirtyMinutes_IsTheEnd()
    {
        // Arrange
        BookingRequest request = BookingRequest.Create(NewGuest(), Stay, RoomSize.Double, 2, FirstHash, MailboxKey, Now);

        // Act
        bool justBefore = request.IsOpenAt(Now.AddMinutes(30).AddTicks(-1));
        bool atTheEnd = request.IsOpenAt(Now.AddMinutes(30));

        // Assert
        Assert.Equal(Now.AddMinutes(30), request.ExpiresAt);
        Assert.True(justBefore);
        Assert.False(atTheEnd);
    }

    /// <summary>Negative: a value that is not a SHA-256 hash as 64 lower-case hex digits is refused.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("gggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggggg")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void Create_HashNotSha256Hex_Throws(string hash)
    {
        // Act and Assert
        Assert.Throws<ArgumentException>(() => BookingRequest.Create(NewGuest(), Stay, RoomSize.Double, 2, hash, MailboxKey, Now));
        Assert.Throws<ArgumentException>(() => BookingRequest.Create(NewGuest(), Stay, RoomSize.Double, 2, FirstHash, hash, Now));
    }

    #endregion

    #region Renew

    /// <summary>State transition: a renewal takes the new answers, token and time, and keeps the key of the mailbox.</summary>
    [Fact]
    public void Renew_NewAnswers_ReplacesAllButTheMailbox()
    {
        // Arrange
        BookingRequest request = BookingRequest.Create(NewGuest(), Stay, RoomSize.Double, 2, FirstHash, MailboxKey, Now);
        DateRange otherStay = new(new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 3));
        Guest other = Guest.Create("Anne", "Hansen", "anna+trip@example.com", "+4587654321", "Sweden", null);

        // Act
        request.Renew(other, otherStay, RoomSize.Suite, 1, SecondHash, Now.AddMinutes(5));

        // Assert
        Assert.Equal((SecondHash, MailboxKey), (request.TokenHash, request.EmailKey));
        Assert.Equal((otherStay, RoomSize.Suite, 1), (request.Period, request.RoomSize, request.Guests));
        Assert.Equal(("Anne", "anna+trip@example.com", (string?)null), (request.FirstName, request.Email, request.PassportNumber));
        Assert.Equal(Now.AddMinutes(35), request.ExpiresAt);
    }

    #endregion

    #region The booking with a new guest

    /// <summary>Positive: a booking for a new guest holds the guest itself and is <c>Pending</c> with a reference.</summary>
    [Fact]
    public void CreateForNewGuest_NewGuest_HoldsTheGuest()
    {
        // Arrange
        Guest guest = NewGuest();
        DateOnly today = new(2026, 1, 1);

        // Act
        Booking booking = Booking.CreateForNewGuest(Stay, 3, guest, today);

        // Assert
        Assert.Same(guest, booking.Guest);
        Assert.Equal((0, 3, BookingStatus.Pending), (booking.GuestId, booking.RoomId, booking.Status));
        Assert.NotEqual(Guid.Empty, booking.Reference);
    }

    /// <summary>Negative: a stored guest is booked with <see cref="Booking.Create"/>, not with a new guest.</summary>
    [Fact]
    public void CreateForNewGuest_StoredGuest_Throws()
    {
        // Arrange
        Guest guest = NewGuest();
        // ! is justified: Entity<int> has the property Id, which nameof checks; an id makes the guest a stored one.
        typeof(Entity<int>).GetProperty(nameof(Entity<int>.Id))!.SetValue(guest, 7);

        // Act and Assert
        Assert.Throws<ArgumentException>(() => Booking.CreateForNewGuest(Stay, 3, guest, new DateOnly(2026, 1, 1)));
    }

    #endregion

    private static Guest NewGuest() => Guest.Create("Anna", "Jensen", "anna@example.com", "+4512345678", "Denmark", "AB123456");
}
