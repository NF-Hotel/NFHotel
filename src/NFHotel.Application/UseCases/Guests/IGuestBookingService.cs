using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases.Guests;

/// <summary>
/// The signed-in guest's own booking (MIL-018, ADR-0049 decisions 3 and 7): read, moved and cancelled without a key in
/// the call, because the booking is the one in the guest token, and answered with the receipt alone.
/// </summary>
public interface IGuestBookingService
{
    #region Public methods

    /// <summary>Reads the receipt of the booking in the guest token.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The receipt, or <see cref="ErrorCodes.Api.GuestUnauthorized"/> without a signed-in guest.</returns>
    Task<Result<BookingReceiptDto>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Cancels the booking in the guest token (BR-11, BR-13).</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The receipt with the new status, the code of the booking when it cannot be cancelled, or
    /// <see cref="ErrorCodes.Api.GuestUnauthorized"/> without a signed-in guest.
    /// </returns>
    Task<Result<BookingReceiptDto>> CancelAsync(CancellationToken cancellationToken = default);

    /// <summary>Moves the booking in the guest token to new dates in the room it has (BR-14, MIL-018).</summary>
    /// <param name="request">The new dates.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The receipt with the new dates, the codes of the booking when the move breaks a rule, or
    /// <see cref="ErrorCodes.Api.GuestUnauthorized"/> without a signed-in guest.
    /// </returns>
    Task<Result<BookingReceiptDto>> RescheduleAsync(GuestRescheduleRequest request, CancellationToken cancellationToken = default);

    #endregion Public methods
}
