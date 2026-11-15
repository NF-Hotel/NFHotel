using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases.Guests;

/// <summary>
/// The guest's way into their own booking (MIL-018, ADR-0047, UC-010): the sign-in with the number and the email of a
/// booking, and the public booking that signs its guest in at once. There is no guest account and no password; the
/// token that both give opens one booking for two hours.
/// </summary>
public interface IGuestAccessService
{
    #region Public methods

    /// <summary>Signs a guest in with the number and the email of their booking.</summary>
    /// <param name="request">The booking key (the digits of the booking number) and the email of the booking.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The guest session; <see cref="ErrorCodes.Api.GuestUnauthorized"/> for a wrong email, an unknown number and a
    /// cancelled booking alike; <see cref="ErrorCodes.Booking.ReceiptEmailRequired"/> without an email; and
    /// <see cref="ErrorCodes.Booking.TooManyLookups"/> after too many failed attempts for the number.
    /// </returns>
    Task<Result<GuestSessionDto>> SignInAsync(BookingReceiptRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Books a stay from the public page for a new guest and signs the guest in to the new booking.
    /// </summary>
    /// <param name="request">The stay, the room type, the party and the guest's own details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The booking with the guest session, or the broken rules of the booking.</returns>
    Task<Result<PublicBookingDto>> BookAsync(CreatePublicBookingRequest request, CancellationToken cancellationToken = default);

    #endregion Public methods
}
