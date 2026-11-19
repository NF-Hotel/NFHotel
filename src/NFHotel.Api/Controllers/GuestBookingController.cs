using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The signed-in guest's own booking over HTTP (ADR-0049 decisions 3 and 7): the routes take no key, because the guest
/// token names the booking, and they answer with the receipt alone. Only a guest token passes, with the permissions of the
/// Guest role that the staff routes held for a guest before (MIL-018); a staff token gets 403.
/// </summary>
[ApiController]
[Route(ApiRoutes.GuestBooking)]
public sealed class GuestBookingController : ControllerBase
{
    #region Fields

    private readonly IGuestBookingService _booking;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the controller.</summary>
    /// <param name="booking">The use cases of the guest's own booking.</param>
    public GuestBookingController(IGuestBookingService booking)
    {
        ArgumentNullException.ThrowIfNull(booking);

        _booking = booking;
    }

    #endregion Constructors

    #region Public methods

    /// <summary>Reads the receipt of the guest's booking.</summary>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200 with the receipt, or the one 401 of <see cref="GuestAuthentication.Refusal"/>.</returns>
    [HttpGet]
    [RequiresGuest(Permissions.ViewOwnGuestData)]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken) =>
        Answer(await _booking.GetAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Cancels the guest's booking.</summary>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200 with the receipt, the code of the booking when it cannot be cancelled, or the one 401.</returns>
    [HttpPost(ApiRoutes.GuestBookingCancel)]
    [RequiresGuest(Permissions.EditOwnBooking)]
    public async Task<IActionResult> CancelAsync(CancellationToken cancellationToken) =>
        Answer(await _booking.CancelAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Moves the guest's booking to new dates in the room it has.</summary>
    /// <param name="request">The new dates; a field that the type does not have, a room among them, is refused with 400 (MIL-086).</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200 with the receipt, the codes of the booking when the move breaks a rule, or the one 401.</returns>
    [HttpPost(ApiRoutes.GuestBookingReschedule)]
    [RequiresGuest(Permissions.EditOwnBooking)]
    public async Task<IActionResult> RescheduleAsync([FromBody] GuestRescheduleRequest request, CancellationToken cancellationToken) =>
        Answer(await _booking.RescheduleAsync(request, cancellationToken).ConfigureAwait(false));

    #endregion Public methods

    #region Private methods

    // A booking that is gone since the token was issued gets the refusal of a guest, with the text of a refused token,
    // not the envelope of a value (ADR-0047 decision 3, ADR-0049 decision 2).
    private IActionResult Answer(Result<BookingReceiptDto> result) =>
        result.Errors.Any(static error => error.Code == ErrorCodes.Api.GuestUnauthorized)
            ? GuestAuthentication.Refusal()
            : this.ToResponse(result);

    #endregion Private methods
}
