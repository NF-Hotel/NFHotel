using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases.Bookings;

/// <summary>
/// A booking on the public site that takes a room only once its email is verified (UC-011, MIL-088, ADR-0052): the guest
/// asks, the API sends a mail with a single-use link, and the button of the page of the link books.
/// </summary>
public interface IBookingRequestService
{
    #region Public methods

    /// <summary>Tells which way a booking on the public site goes (ADR-0052 decision 8).</summary>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>The mode of the public booking.</returns>
    Task<Result<PublicBookingModeDto>> GetModeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asks for a booking (ADR-0052 decision 1): checks it as a public booking is checked, stores a request that holds no
    /// room, replaces an open request of the same email, and sends the mail with the link.
    /// </summary>
    /// <param name="request">The stay, the room type, the party and the new guest.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>
    /// The time the link lapses; or <see cref="ErrorCodes.Booking.TooManyMails"/>,
    /// <see cref="ErrorCodes.Booking.MailPaused"/>, <see cref="ErrorCodes.Booking.MailNotSent"/>, or the codes of the
    /// public booking. With the verification off, <see cref="ErrorCodes.Booking.NotFound"/>.
    /// </returns>
    Task<Result<BookingRequestReceiptDto>> AskAsync(CreatePublicBookingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a request with the token of its link (ADR-0052 decision 5): books it, signs the guest in, and sends the
    /// mail with the reference.
    /// </summary>
    /// <param name="request">The token of the link.</param>
    /// <param name="cancellationToken">Cancels the call.</param>
    /// <returns>
    /// The receipt with the guest session; <see cref="ErrorCodes.Booking.VerificationLinkInvalid"/> for a token that is
    /// unknown, used or lapsed; <see cref="ErrorCodes.Booking.RoomTypeNotAvailable"/> when the room went; or the codes of
    /// the public booking. With the verification off, <see cref="ErrorCodes.Booking.NotFound"/>.
    /// </returns>
    Task<Result<PublicBookingDto>> VerifyAsync(VerifyBookingRequest request, CancellationToken cancellationToken = default);

    #endregion Public methods
}
