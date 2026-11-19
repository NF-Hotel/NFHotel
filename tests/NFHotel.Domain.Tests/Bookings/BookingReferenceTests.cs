using NFHotel.Domain.Entities;
using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Bookings;

/// <summary>
/// The booking reference of ADR-0049 (MIL-085, BR-109): a version 4 UUID that names a booking, made by
/// <c>Guid.NewGuid()</c>, which on Linux draws its 122 random bits from the secure random generator of the operating
/// system. Its text is the usual form of 36 characters in lower case, and only that form is read back.
/// </summary>
public sealed class BookingReferenceTests
{
    #region Fields

    /// <summary>How many references in a row must all differ (MIL-085 criterion 2).</summary>
    private const int ReferencesInARow = 10_000;

    /// <summary>The value of the version field of a random UUID (RFC 9562, section 5.4).</summary>
    private const int RandomVersion = 4;

    #endregion Fields

    #region New

    /// <summary>
    /// MIL-085 criterion 2: 10,000 references made in a row by <see cref="BookingReference.New"/>, which is
    /// <c>Guid.NewGuid()</c>, are all different, and each is a version 4 UUID with the variant of RFC 9562.
    /// </summary>
    [Fact]
    public void New_TenThousandInARow_AreAllDifferentVersion4UuidsWithTheRfcVariant()
    {
        // Arrange
        HashSet<Guid> seen = [];

        // Act
        for (int index = 0; index < ReferencesInARow; index++)
        {
            Guid reference = BookingReference.New();

            // Assert
            Assert.Equal(RandomVersion, reference.Version);
            Assert.True(IsRfcVariant(reference), $"The variant of {reference} is not the one of RFC 9562.");
            Assert.True(seen.Add(reference), $"{reference} came twice.");
        }

        Assert.Equal(ReferencesInARow, seen.Count);
    }

    /// <summary>MIL-085 decision 1: a new booking gets its reference when it is made, a version 4 UUID.</summary>
    [Fact]
    public void Create_NewBooking_HasAVersion4Reference()
    {
        // Arrange and act
        Booking booking = Booking.Create(BookingFactory.StandardPeriod, BookingFactory.RoomId, BookingFactory.GuestId, BookingFactory.Today);

        // Assert
        Assert.NotEqual(Guid.Empty, booking.Reference);
        Assert.Equal(RandomVersion, booking.Reference.Version);
        Assert.True(IsRfcVariant(booking.Reference));
    }

    /// <summary>Two bookings made one after the other do not get references that follow from each other.</summary>
    [Fact]
    public void Create_TwoBookings_GetDifferentReferences()
    {
        // Arrange and act
        Booking first = Booking.Create(BookingFactory.StandardPeriod, BookingFactory.RoomId, BookingFactory.GuestId, BookingFactory.Today);
        Booking second = Booking.Create(BookingFactory.StandardPeriod, BookingFactory.RoomId, BookingFactory.GuestId, BookingFactory.Today);

        // Assert
        Assert.NotEqual(first.Reference, second.Reference);
    }

    #endregion New

    #region Format

    /// <summary>ADR-0049 decision 1: the text of a reference is 36 characters, lower case, with hyphens.</summary>
    [Fact]
    public void Format_AnyReference_IsTheLowerCaseFormWithHyphens()
    {
        // Arrange
        Guid reference = Guid.Parse("3F2504E0-4F89-41D3-9A0C-0305E82C3301");

        // Act
        string text = BookingReference.Format(reference);

        // Assert
        Assert.Equal("3f2504e0-4f89-41d3-9a0c-0305e82c3301", text);
        Assert.Equal(BookingReference.TextLength, text.Length);
    }

    /// <summary>A reference that is formatted reads back as the same reference.</summary>
    [Fact]
    public void Format_NewReference_ReadsBackAsTheSameReference()
    {
        // Arrange
        Guid reference = BookingReference.New();

        // Act
        bool read = BookingReference.TryParse(BookingReference.Format(reference), out Guid back);

        // Assert
        Assert.True(read);
        Assert.Equal(reference, back);
    }

    #endregion Format

    #region TryParse

    /// <summary>
    /// ADR-0049 decision 1: the usual form is read in any letter case and with spaces around it, as a guest copies it.
    /// </summary>
    [Theory]
    [InlineData("3f2504e0-4f89-41d3-9a0c-0305e82c3301")]
    [InlineData("3F2504E0-4F89-41D3-9A0C-0305E82C3301")]
    [InlineData("  3f2504e0-4f89-41d3-9a0c-0305e82c3301  ")]
    [InlineData("\t3F2504e0-4f89-41D3-9a0c-0305E82C3301\n")]
    public void TryParse_TheUsualFormInAnyCaseOrWithSpaces_IsRead(string text)
    {
        // Act
        bool read = BookingReference.TryParse(text, out Guid reference);

        // Assert
        Assert.True(read);
        Assert.Equal(Guid.Parse("3f2504e0-4f89-41d3-9a0c-0305e82c3301"), reference);
    }

    /// <summary>
    /// ADR-0049 decisions 1 and 5: the numbers of the old kind, the other text forms of a UUID, a UUID of another
    /// version or variant, the empty UUID, and anything else are unreadable.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("FLZ-000123")]
    [InlineData("flz-123")]
    [InlineData("000123")]
    [InlineData("123")]
    [InlineData("00000000-0000-0000-0000-000000000000")] // the empty UUID
    [InlineData("3f2504e04f8941d39a0c0305e82c3301")] // 32 digits without hyphens
    [InlineData("{3f2504e0-4f89-41d3-9a0c-0305e82c3301}")] // in braces
    [InlineData("(3f2504e0-4f89-41d3-9a0c-0305e82c3301)")] // in parentheses
    [InlineData("3f2504e0-4f89-11d3-9a0c-0305e82c3301")] // version 1
    [InlineData("018f2504-4f89-71d3-9a0c-0305e82c3301")] // version 7, which carries the time
    [InlineData("3f2504e0-4f89-41d3-ca0c-0305e82c3301")] // the variant of Microsoft
    [InlineData("3f2504e0-4f89-41d3-9a0c-0305e82c330")] // one digit short
    [InlineData("3f2504e0-4f89-41d3-9a0c-0305e82c33011")] // one digit too many
    [InlineData("3f2504e0-4f89-41d3-9a0c-0305e82c33g1")] // not a hexadecimal digit
    [InlineData("3f2504e0-4f89-41d3-9a0c-0305e82c3301 FLZ-000123")]
    public void TryParse_AnythingElse_IsUnreadable(string? text)
    {
        // Act
        bool read = BookingReference.TryParse(text, out Guid reference);

        // Assert
        Assert.False(read);
        Assert.Equal(Guid.Empty, reference);
    }

    /// <summary>A very long text is refused without being read further than its length.</summary>
    [Fact]
    public void TryParse_VeryLongText_IsUnreadable()
    {
        // Arrange
        string text = new('a', 100_000);

        // Act
        bool read = BookingReference.TryParse(text, out Guid reference);

        // Assert
        Assert.False(read);
        Assert.Equal(Guid.Empty, reference);
    }

    #endregion TryParse

    #region Private methods

    /// <summary>The variant of RFC 9562 has the bits 10 at the top of the ninth byte (section 4.1).</summary>
    private static bool IsRfcVariant(Guid reference) => (reference.Variant & 0b1100) == 0b1000;

    #endregion Private methods
}
