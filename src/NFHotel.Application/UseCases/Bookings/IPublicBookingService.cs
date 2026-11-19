using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases.Bookings;

/// <summary>
/// The booking from the public page (UC-001), apart from the booking use cases of staff (ADR-0049 decision 3). Its
/// answer holds the key of the new booking, which only the guest sign-in in the API reads, to issue the guest token;
/// the guest gets the receipt (<see cref="NFHotel.Application.UseCases.Guests.IGuestAccessService.BookAsync"/>).
/// </summary>
public interface IPublicBookingService
{
    /// <summary>
    /// Creates a booking through the public guest booking flow: the guest chooses a room type and the service picks
    /// a free room of that type. Only new guest details are accepted.
    /// </summary>
    /// <param name="request">The stay, the room type, the party and the new guest.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The created booking, <see cref="ErrorCodes.Booking.RoomTypeNotAvailable"/> when no room of the type is
    /// free, or the validation errors.</returns>
    Task<Result<BookingDetailsDto>> CreatePublicAsync(
        CreatePublicBookingRequest request,
        CancellationToken cancellationToken = default);
}
