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
    #region Public methods

    /// <summary>
    /// Creates a booking through the public guest booking flow: the guest chooses a room type and the service picks
    /// a free room of that type. Only new guest details are accepted. The guest and the booking are stored in one save,
    /// so a booking that fails leaves no guest behind (MIL-088).
    /// </summary>
    /// <param name="request">The stay, the room type, the party and the new guest.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>The created booking, <see cref="ErrorCodes.Booking.RoomTypeNotAvailable"/> when no room of the type is
    /// free, <see cref="ErrorCodes.Booking.VerificationRequired"/> when a public booking must be verified by email
    /// (MIL-088, ADR-0052 decision 8), or the validation errors.</returns>
    Task<Result<BookingDetailsDto>> CreatePublicAsync(
        CreatePublicBookingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks a public booking as <see cref="CreatePublicAsync"/> checks it, and stores nothing (MIL-088, ADR-0052
    /// decision 1): the rules of the stay and the party, the limit per email, the rules of the guest and a free room of
    /// the size.
    /// </summary>
    /// <param name="request">The stay, the room type, the party and the new guest.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>Success, or the codes of <see cref="CreatePublicAsync"/>.</returns>
    Task<Result> CheckPublicAsync(
        CreatePublicBookingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Books a verified request (MIL-088, ADR-0052 decision 5): the checks of <see cref="CreatePublicAsync"/>, then the
    /// guest and the booking in one save. Unlike <see cref="CreatePublicAsync"/> it books with the verification on; only
    /// the verification of a claimed request calls it.
    /// </summary>
    /// <param name="request">The stay, the room type, the party and the new guest, as the request kept them.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// The created booking; <see cref="ErrorCodes.Booking.RoomTypeNotAvailable"/> when no room of the type is free or the
    /// last one went to another guest at the save; or the codes of the rules.
    /// </returns>
    Task<Result<BookingDetailsDto>> BookVerifiedAsync(
        CreatePublicBookingRequest request,
        CancellationToken cancellationToken = default);

    #endregion Public methods
}
