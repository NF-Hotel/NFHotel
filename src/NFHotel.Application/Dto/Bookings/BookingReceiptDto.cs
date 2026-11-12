using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// What the public receipt shows of a booking, and nothing more (BR-C-05, BR-C-06, MIL-079): no guest, no price and no
/// room number. The guest's lookup answers with this type only, so the guest data of a booking never leaves the API for
/// the receipt.
/// </summary>
/// <param name="BookingNumber">The booking number, for example <c>FLZ-000123</c>.</param>
/// <param name="StartDate">Arrival date.</param>
/// <param name="EndDate">Departure date.</param>
/// <param name="Nights">Number of nights.</param>
/// <param name="RoomSize">The room type; the room number is given at check-in.</param>
/// <param name="Status">The status of the booking.</param>
public sealed record BookingReceiptDto(
    string BookingNumber,
    DateOnly StartDate,
    DateOnly EndDate,
    int Nights,
    RoomSize RoomSize,
    BookingStatus Status)
{
    /// <summary>Takes the receipt fields of a booking: the one mapping for the lookup and for the page after a booking.</summary>
    /// <param name="details">The booking.</param>
    /// <returns>The receipt of the booking.</returns>
    public static BookingReceiptDto From(BookingDetailsDto details)
    {
        ArgumentNullException.ThrowIfNull(details);

        return new BookingReceiptDto(
            details.BookingNumber,
            details.StartDate,
            details.EndDate,
            details.Nights,
            details.Room.Size,
            details.Status);
    }
}
