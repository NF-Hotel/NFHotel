using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>The booking use cases over HTTP: one endpoint per method of <see cref="IBookingService"/>.</summary>
[ApiController]
[Route(ApiRoutes.Bookings)]
public sealed class BookingsController : ControllerBase
{
    private readonly IBookingService _bookings;

    /// <summary>Creates the controller.</summary>
    /// <param name="bookings">The booking use cases.</param>
    public BookingsController(IBookingService bookings)
    {
        ArgumentNullException.ThrowIfNull(bookings);
        _bookings = bookings;
    }

    /// <summary>Gets one booking, for staff who may see the bookings (MIL-079).</summary>
    /// <param name="id">The booking key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The details, 404, or 401 and 403 for a caller without the permission.</returns>
    [HttpGet(ApiRoutes.ById)]
    [RequiresPermission(Permissions.BookingsView)]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.GetByIdAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Looks a booking up for its guest, by the number and the email of the booking (MIL-079). Public in the sense of the
    /// booking flow: it needs the token of the web application, no staff token.
    /// </summary>
    /// <param name="request">The booking key and the email, in the body so the email stays out of access logs.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>Only the receipt; the same 404 for a wrong email and an unknown number; 422 without an email; 429 after too many failed lookups of the number.</returns>
    [HttpPost(ApiRoutes.PublicReceipt)]
    public async Task<IActionResult> LookUpReceiptAsync([FromBody] BookingReceiptRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.LookUpReceiptAsync(request, cancellationToken).ConfigureAwait(false));

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
    /// Creates a public booking for a new guest in a free room of the chosen type.
    /// </summary>
    /// <param name="request">The stay, the room type, the party and the new guest.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The created booking or validation errors.</returns>
    [HttpPost(ApiRoutes.PublicBooking)]
    public async Task<IActionResult> CreatePublicAsync(
        [FromBody] CreatePublicBookingRequest request,
        CancellationToken cancellationToken) =>
        this.ToResponse(
            await _bookings.CreatePublicAsync(request, cancellationToken)
                .ConfigureAwait(false));

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
