using NFHotel.Domain.Entities;
using NFHotel.Domain.Errors;
using NFHotel.Domain.Tests.TestSupport;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Bookings;

/// <summary>
/// Modelvalidering og beregnede felter på <c>Booking</c>.
/// Dækker BR-01, BR-44, BR-45, BR-48, BR-104, BR-105, BR-106, BR-109 og BR-117.
/// </summary>
public class BookingValidationTests
{
    // --------------------------------------------------------
    // OPRETTELSE (BR-01, BR-48)
    // --------------------------------------------------------

    /// <summary>
    /// BR-01, BR-48: en booking oprettes altid som <c>Pending</c>, med den valgte periode,
    /// det valgte rum og gæstens id. Der findes ingen anden startstatus.
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
    /// BR-44, BR-104: ankomstdatoen må ikke ligge før dags dato. Datodelen sammenlignes,
    /// så samme dag er tilladt — "i dag" leveres udefra (A-05), aldrig af <c>DateTime.Now</c>.
    /// </summary>
    [Theory]
    [InlineData("2026-01-01")] // ankomsten er i dag
    [InlineData("2025-12-31")] // ankomsten er i morgen
    [InlineData("2020-06-15")] // ankomsten ligger år ude i fremtiden
    public void Create_StartDateOnOrAfterToday_IsAccepted(string today)
    {
        // Arrange
        var period = BookingFactory.StandardPeriod;

        // Act
        var booking = Booking.Create(period, BookingFactory.RoomId, BookingFactory.GuestId, TestDate.Of(today));

        // Assert
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    /// <summary>BR-44, BR-104: en ankomst i fortiden afvises.</summary>
    [Theory]
    [InlineData("2026-01-02")] // dagen efter ankomsten
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

    /// <summary>BR-45, BR-105: en booking uden rum kan ikke oprettes.</summary>
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

    /// <summary>BR-106: en booking uden gæst kan ikke oprettes.</summary>
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

    // --------------------------------------------------------
    // BEREGNEDE FELTER (BR-109, BR-117)
    // --------------------------------------------------------

    /// <summary>
    /// BR-109, BR-117: antal nætter er periodens længde i overnatninger — ikke antal
    /// berørte kalenderdage. Kalenderens bjælkebredde bygger på samme tal.
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
    /// BR-109: bookingnummeret vises som <c>FLZ-</c> plus id'et nulpolstret til seks
    /// cifre. Et id på over seks cifre afkortes ikke.
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
}