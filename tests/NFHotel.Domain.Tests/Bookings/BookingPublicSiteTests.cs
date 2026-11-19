using NFHotel.Domain.Entities;
using NFHotel.Domain.ValueObjects;

namespace NFHotel.Domain.Tests.Bookings;

/// <summary>
/// The mark of a booking made on the public site (MIL-087, ADR-0051 decision 3), which the limit per email counts: a new
/// booking is not marked, and only <see cref="Booking.MarkMadeOnPublicSite"/> marks it.
/// </summary>
public sealed class BookingPublicSiteTests
{
    private static readonly DateOnly Today = new(2026, 1, 1);

    /// <summary>Positive: a booking is made without the mark, as staff make it.</summary>
    [Fact]
    public void Create_NewBooking_IsNotMarked()
    {
        // Act
        Booking booking = Booking.Create(new DateRange(Today, Today.AddDays(2)), 1, 1, Today);

        // Assert
        Assert.False(booking.MadeOnPublicSite);
    }

    /// <summary>State transition: the mark sets the flag, and a second mark keeps it.</summary>
    [Fact]
    public void MarkMadeOnPublicSite_NewBooking_IsMarked()
    {
        // Arrange
        Booking booking = Booking.Create(new DateRange(Today, Today.AddDays(2)), 1, 1, Today);

        // Act
        booking.MarkMadeOnPublicSite();
        booking.MarkMadeOnPublicSite();

        // Assert
        Assert.True(booking.MadeOnPublicSite);
    }
}
