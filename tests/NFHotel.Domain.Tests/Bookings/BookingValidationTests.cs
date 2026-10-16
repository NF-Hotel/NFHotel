using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Bookings;

/// <summary>
/// Model validation and computed fields on <c>Booking</c>.
/// Covers BR-01, BR-44, BR-45, BR-48, BR-104, BR-105, BR-106, BR-109 and BR-117.
/// </summary>
public class BookingValidationTests
{
    #region Creation (BR-01, BR-48)

    /// <summary>
    /// BR-01, BR-48: a booking is always created as <c>Pending</c>, with the chosen period,
    /// the chosen room and the id of the guest. There is no other start status.
    /// </summary>
    [Fact]
    public void Create_ValidInput_StartsAsPendingWithGivenPeriodRoomAndGuest()
    {
        // Arrange
        var period = BookingFactory.StandardPeriod;

        // Act
        var booking = Booking.Create(period, BookingFactory.RoomId, BookingFactory.GuestId, BookingFactory.Today);

        // Assert
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(period, booking.Period);
        Assert.Equal(BookingFactory.RoomId, booking.RoomId);
        Assert.Equal(BookingFactory.GuestId, booking.GuestId);
        Assert.Null(booking.CheckInTime);
        Assert.Null(booking.CheckOutTime);
        Assert.Null(booking.CheckOutDate);
    }

    /// <summary>
    /// BR-44, BR-104: the arrival date must not lie before today's date. The date part is compared,
    /// so the same day is allowed — "today" is supplied from outside (A-05), never by <c>DateTime.Now</c>.
    /// </summary>
    [Theory]
    [InlineData("2026-01-01")] // the arrival is today
    [InlineData("2025-12-31")] // the arrival is tomorrow
    [InlineData("2020-06-15")] // the arrival is years into the future
    public void Create_StartDateOnOrAfterToday_IsAccepted(string today)
    {
        // Arrange
        var period = BookingFactory.StandardPeriod;

        // Act
        var booking = Booking.Create(period, BookingFactory.RoomId, BookingFactory.GuestId, TestDate.Of(today));

        // Assert
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    /// <summary>BR-44, BR-104: an arrival in the past is rejected.</summary>
    [Theory]
    [InlineData("2026-01-02")] // the day after arrival
    [InlineData("2026-06-01")]
    public void Create_StartDateInThePast_ThrowsStartDateInPast(string today)
    {
        // Arrange
        var period = BookingFactory.StandardPeriod;

        // Act
        var exception = Assert.Throws<DomainException>(
            () => Booking.Create(period, BookingFactory.RoomId, BookingFactory.GuestId, TestDate.Of(today)));

        // Assert
        Assert.Equal(BookingErrors.StartDateInPast, exception.Code);
    }

    /// <summary>BR-45, BR-105: a booking without a room cannot be created.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithoutRoom_ThrowsRoomRequired(int roomId)
    {
        // Arrange
        var period = BookingFactory.StandardPeriod;

        // Act
        var exception = Assert.Throws<DomainException>(
            () => Booking.Create(period, roomId, BookingFactory.GuestId, BookingFactory.Today));

        // Assert
        Assert.Equal(BookingErrors.RoomRequired, exception.Code);
    }

    /// <summary>BR-106: a booking without a guest cannot be created.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithoutGuest_ThrowsGuestRequired(int guestId)
    {
        // Arrange
        var period = BookingFactory.StandardPeriod;

        // Act
        var exception = Assert.Throws<DomainException>(
            () => Booking.Create(period, BookingFactory.RoomId, guestId, BookingFactory.Today));

        // Assert
        Assert.Equal(BookingErrors.GuestRequired, exception.Code);
    }

    #endregion

    #region Computed fields (BR-109, BR-117)

    /// <summary>
    /// BR-109, BR-117: the number of nights is the length of the period in overnight stays — not the number
    /// of calendar days touched. The width of the calendar bar is based on the same number.
    /// </summary>
    [Theory]
    [InlineData("2026-01-01", "2026-01-02", 1)]
    [InlineData("2026-01-01", "2026-01-10", 9)]
    [InlineData("2026-01-28", "2026-02-02", 5)]
    public void NumberOfNights_CountsOvernightStays(string start, string end, int expectedNights)
    {
        // Arrange
        var period = new DateRange(TestDate.Of(start), TestDate.Of(end));

        // Act
        var booking = Booking.Create(period, BookingFactory.RoomId, BookingFactory.GuestId, TestDate.Of(start));

        // Assert
        Assert.Equal(expectedNights, booking.NumberOfNights);
    }

    /// <summary>
    /// BR-109: the booking number is shown as <c>FLZ-</c> plus the id zero-padded to six
    /// digits. An id of more than six digits is not truncated.
    /// </summary>
    [Theory]
    [InlineData(0, "FLZ-000000")]
    [InlineData(1, "FLZ-000001")]
    [InlineData(123, "FLZ-000123")]
    [InlineData(999999, "FLZ-999999")]
    [InlineData(1234567, "FLZ-1234567")]
    public void BookingNumber_IsPrefixedAndPaddedToSixDigits(int bookingId, string expected)
    {
        // Arrange
        var booking = BookingFactory.WithBookingId(BookingFactory.Pending(), bookingId);

        // Act
        var bookingNumber = booking.BookingNumber;

        // Assert
        Assert.Equal(expected, bookingNumber);
    }

    #endregion
}