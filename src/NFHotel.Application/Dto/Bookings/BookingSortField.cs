namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// A field the booking overview can be sorted on (BR-32).
/// </summary>
/// <remarks>
/// An enum and not a string: a typo in a column name must be a compiler error, not
/// a silently unsorted list.
/// </remarks>
public enum BookingSortField
{
    /// <summary>Booking number — corresponds to the id order (BR-109).</summary>
    BookingNumber = 0,

    /// <summary>The full name of the guest (BR-25).</summary>
    GuestName = 1,

    /// <summary>Room number.</summary>
    RoomNumber = 2,

    /// <summary>Arrival date.</summary>
    StartDate = 3,

    /// <summary>Booked departure date.</summary>
    EndDate = 4,

    /// <summary>The status of the booking.</summary>
    Status = 5,

    /// <summary>Number of nights (BR-117).</summary>
    Nights = 6
}
