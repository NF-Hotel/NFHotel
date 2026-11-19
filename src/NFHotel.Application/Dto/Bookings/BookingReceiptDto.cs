using NFHotel.Domain.ValueObjects;

namespace NFHotel.Application.Dto.Bookings;

/// <summary>
/// What the public receipt shows of a booking, and nothing more (BR-C-05, BR-C-06, MIL-079, ADR-0049 decision 7): no
/// key, no guest, no price and no room number. The public booking and every guest operation answer with this type only,
/// so the guest data of a booking never leaves the API for a guest.
/// </summary>
/// <param name="Reference">The reference of the booking (BR-109, ADR-0049).</param>
/// <param name="StartDate">Arrival date.</param>
/// <param name="EndDate">Departure date.</param>
/// <param name="Nights">Number of nights.</param>
/// <param name="RoomSize">The room type; the room number is given at check-in.</param>
/// <param name="Status">The status of the booking.</param>
public sealed record BookingReceiptDto(
    Guid Reference,
    DateOnly StartDate,
    DateOnly EndDate,
    int Nights,
    RoomSize RoomSize,
    BookingStatus Status)
{
    /// <summary>Takes the receipt fields of a booking: the one mapping for the public booking and the guest operations.</summary>
    /// <param name="details">The booking.</param>
    /// <returns>The receipt of the booking.</returns>
    public static BookingReceiptDto From(BookingDetailsDto details)
    {
        ArgumentNullException.ThrowIfNull(details);

        return new BookingReceiptDto(
            details.Reference,
            details.StartDate,
            details.EndDate,
            details.Nights,
            details.Room.Size,
            details.Status);
    }
}
