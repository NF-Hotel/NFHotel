using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The booking use cases of staff over HTTP: one endpoint per method of <see cref="IBookingService"/>; no route here
/// admits a guest token (ADR-0049 decision 3). The public booking answers with the receipt and the guest session of
/// <see cref="IGuestAccessService"/>, so the guest who has just booked is signed in (MIL-018) without seeing the key.
/// Since MIL-088 a booking on the public site may instead be asked for and verified by email through
/// <see cref="IBookingRequestService"/> (ADR-0052): the mode tells the web application which way holds.
/// </summary>
[ApiController]
[Route(ApiRoutes.Bookings)]
public sealed class BookingsController : ControllerBase
{
    private readonly IBookingService _bookings;
    private readonly IGuestAccessService _guestAccess;
    private readonly IBookingRequestService _requests;

    /// <summary>Creates the controller.</summary>
    /// <param name="bookings">The booking use cases.</param>
    /// <param name="guestAccess">The public booking that signs its guest in (MIL-018).</param>
    /// <param name="requests">The booking request and its verification by email (MIL-088).</param>
    public BookingsController(IBookingService bookings, IGuestAccessService guestAccess, IBookingRequestService requests)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        ArgumentNullException.ThrowIfNull(guestAccess);
        ArgumentNullException.ThrowIfNull(requests);
        _bookings = bookings;
        _guestAccess = guestAccess;
        _requests = requests;
    }

    /// <summary>Gets one booking, for staff who may see the bookings (MIL-079).</summary>
    /// <param name="id">The booking key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The details, 404, or 401 and 403 for a caller without the permission.</returns>
    [HttpGet(ApiRoutes.ById)]
    [RequiresPermission(Permissions.BookingsView)]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.GetByIdAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Lists the bookings of a period.</summary>
    /// <param name="query">The period, the search text, the sorting and whether to include cancelled bookings.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The rows of the overview.</returns>
    [HttpPost(ApiRoutes.Overview)]
    [RequiresPermission(Permissions.BookingsView)]
    public async Task<IActionResult> GetOverviewAsync([FromBody] BookingOverviewQuery query, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.GetOverviewAsync(query, cancellationToken).ConfigureAwait(false));

    /// <summary>Finds the rooms that are free in a period.</summary>
    /// <param name="checkInDate">Arrival date.</param>
    /// <param name="checkOutDate">Departure date.</param>
    /// <param name="excludeBookingId">A booking to leave out, so it does not block its own room; a key below one is refused.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The free rooms.</returns>
    [HttpGet(ApiRoutes.AvailableRooms)]
    [SenderLimit(SenderLimitKind.Search)]
    public async Task<IActionResult> GetAvailableRoomsAsync(
        [FromQuery] DateOnly checkInDate,
        [FromQuery] DateOnly checkOutDate,
        [FromQuery, Range(1, int.MaxValue)] int? excludeBookingId,
        CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings
            .GetAvailableRoomsAsync(checkInDate, checkOutDate, excludeBookingId, cancellationToken)
            .ConfigureAwait(false));

    /// <summary>Counts the free rooms of each room size, night by night.</summary>
    /// <param name="from">The first night.</param>
    /// <param name="to">The day after the last night.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>One entry per night.</returns>
    [HttpGet(ApiRoutes.Availability)]
    [RequiresPermission(Permissions.BookingsView)]
    public async Task<IActionResult> GetAvailabilityAsync(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.GetAvailabilityAsync(from, to, cancellationToken).ConfigureAwait(false));

    /// <summary>Creates a booking.</summary>
    /// <param name="request">The period, the room and the guest.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The new booking.</returns>
    [HttpPost]
    [RequiresPermission(Permissions.CreateBooking)]
    public async Task<IActionResult> CreateAsync([FromBody] CreateBookingRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.CreateAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Creates a public booking for a new guest in a free room of the chosen type, and signs the guest in to it (MIL-018).
    /// With the verification by email on, it answers 409 <c>booking.verification_required</c>, so no caller books
    /// around the verification (MIL-088, ADR-0052 decision 8).
    /// </summary>
    /// <param name="request">The stay, the room type, the party and the new guest.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The receipt with the guest session, or validation errors (ADR-0049 decision 7).</returns>
    [HttpPost(ApiRoutes.PublicBooking)]
    [SenderLimit(SenderLimitKind.BookingRequest)]
    [BookingFlowEvent(BookingFlowStep.PublicBooking)]
    public async Task<IActionResult> CreatePublicAsync(
        [FromBody] CreatePublicBookingRequest request,
        CancellationToken cancellationToken) =>
        this.ToResponse(
            await _guestAccess.BookAsync(request, cancellationToken)
                .ConfigureAwait(false));

    /// <summary>Tells which way a booking on the public site goes: at once, or asked for and verified by email (MIL-088).</summary>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The mode of the public booking (ADR-0052 decision 8).</returns>
    [HttpGet(ApiRoutes.PublicBookingMode)]
    public async Task<IActionResult> GetPublicBookingModeAsync(CancellationToken cancellationToken) =>
        this.ToResponse(await _requests.GetModeAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Asks for a booking on the public site (MIL-088, ADR-0052 decision 1): checks it as a public booking is checked,
    /// stores a request that holds no room and sends the mail with the single-use link. The limits per address count it
    /// as a booking request; the use case writes the events of the limits per mailbox and of the mail itself.
    /// </summary>
    /// <param name="request">The stay, the room type, the party and the new guest.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>
    /// 202 with the time the link lapses; 429 <c>booking.too_many_mails</c> or <c>booking.too_many_requests</c>; 503
    /// <c>booking.mail_not_sent</c> or <c>booking.mail_paused</c>; the codes of the public booking; 404 with the
    /// verification off.
    /// </returns>
    [HttpPost(ApiRoutes.PublicBookingRequests)]
    [SenderLimit(SenderLimitKind.BookingRequest)]
    public async Task<IActionResult> AskPublicBookingAsync(
        [FromBody] CreatePublicBookingRequest request,
        CancellationToken cancellationToken) =>
        this.ToAcceptedResponse(await _requests.AskAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Verifies a booking request with the token of its link (MIL-088, ADR-0052 decision 5): books it in a free room of
    /// the size and signs the guest in, as a public booking does. The token comes in the body, never in the address.
    /// </summary>
    /// <param name="request">The token of the link.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>
    /// The receipt with the guest session; 422 <c>booking.verification_link_invalid</c> for a link that is unknown, used
    /// or lapsed; 409 <c>booking.room_type_not_available</c> when the room went; 404 with the verification off.
    /// </returns>
    [HttpPost(ApiRoutes.PublicBookingVerify)]
    public async Task<IActionResult> VerifyPublicBookingAsync(
        [FromBody] VerifyBookingRequest request,
        CancellationToken cancellationToken) =>
        this.ToResponse(await _requests.VerifyAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Confirms a booking.</summary>
    /// <param name="id">The booking key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The booking after the change.</returns>
    [HttpPost(ApiRoutes.BookingAction.Confirm)]
    [RequiresPermission(Permissions.EditOtherBookings)]
    public async Task<IActionResult> ConfirmAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.ConfirmAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Checks the guest in.</summary>
    /// <param name="id">The booking key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The booking after the change.</returns>
    [HttpPost(ApiRoutes.BookingAction.CheckIn)]
    [RequiresPermission(Permissions.EditOtherBookings)]
    public async Task<IActionResult> CheckInAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.CheckInAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Checks the guest out.</summary>
    /// <param name="id">The booking key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The booking after the change.</returns>
    [HttpPost(ApiRoutes.BookingAction.CheckOut)]
    [RequiresPermission(Permissions.EditOtherBookings)]
    public async Task<IActionResult> CheckOutAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.CheckOutAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Cancels a booking.</summary>
    /// <param name="id">The booking key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The booking after the change.</returns>
    [HttpPost(ApiRoutes.BookingAction.Cancel)]
    [RequiresPermission(Permissions.EditOtherBookings)]
    public async Task<IActionResult> CancelAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.CancelAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Moves a booking to other dates or another room.</summary>
    /// <param name="request">The booking and its new period and room.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The booking after the change.</returns>
    [HttpPost(ApiRoutes.Reschedule)]
    [RequiresPermission(Permissions.EditOtherBookings)]
    public async Task<IActionResult> RescheduleAsync([FromBody] RescheduleBookingRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.RescheduleAsync(request, cancellationToken).ConfigureAwait(false));
}
