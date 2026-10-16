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

    /// <summary>Gets one booking.</summary>
    /// <param name="id">The booking key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The details, or 404.</returns>
    [HttpGet(ApiRoutes.ById)]
    public async Task<IActionResult> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.GetByIdAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Lists the bookings of a period.</summary>
    /// <param name="query">The period, the search text, the sorting and whether to include cancelled bookings.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The rows of the overview.</returns>
    [HttpPost(ApiRoutes.Overview)]
    [RequiresPermission(Permission.BookingsView)]
    public async Task<IActionResult> GetOverviewAsync([FromBody] BookingOverviewQuery query, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.GetOverviewAsync(query, cancellationToken).ConfigureAwait(false));

    /// <summary>Finds the rooms that are free in a period.</summary>
    /// <param name="checkInDate">Arrival date.</param>
    /// <param name="checkOutDate">Departure date.</param>
    /// <param name="excludeBookingId">A booking to leave out, so it does not block its own room.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The free rooms.</returns>
    [HttpGet(ApiRoutes.AvailableRooms)]
    public async Task<IActionResult> GetAvailableRoomsAsync(
        [FromQuery] DateOnly checkInDate,
        [FromQuery] DateOnly checkOutDate,
        [FromQuery] int? excludeBookingId,
        CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings
            .GetAvailableRoomsAsync(checkInDate, checkOutDate, excludeBookingId, cancellationToken)
            .ConfigureAwait(false));

    /// <summary>Creates a booking.</summary>
    /// <param name="request">The period, the room and the guest.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The new booking.</returns>
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateBookingRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.CreateAsync(request, cancellationToken).ConfigureAwait(false));

    /// <summary>Confirms a booking.</summary>
    /// <param name="id">The booking key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The booking after the change.</returns>
    [HttpPost(ApiRoutes.BookingAction.Confirm)]
    [RequiresPermission(Permission.BookingsManage)]
    public async Task<IActionResult> ConfirmAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.ConfirmAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Checks the guest in.</summary>
    /// <param name="id">The booking key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The booking after the change.</returns>
    [HttpPost(ApiRoutes.BookingAction.CheckIn)]
    [RequiresPermission(Permission.BookingsManage)]
    public async Task<IActionResult> CheckInAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.CheckInAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Checks the guest out.</summary>
    /// <param name="id">The booking key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The booking after the change.</returns>
    [HttpPost(ApiRoutes.BookingAction.CheckOut)]
    [RequiresPermission(Permission.BookingsManage)]
    public async Task<IActionResult> CheckOutAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.CheckOutAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Cancels a booking.</summary>
    /// <param name="id">The booking key.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The booking after the change.</returns>
    [HttpPost(ApiRoutes.BookingAction.Cancel)]
    [RequiresPermission(Permission.BookingsManage)]
    public async Task<IActionResult> CancelAsync(int id, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.CancelAsync(id, cancellationToken).ConfigureAwait(false));

    /// <summary>Moves a booking to other dates or another room.</summary>
    /// <param name="request">The booking and its new period and room.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>The booking after the change.</returns>
    [HttpPost(ApiRoutes.Reschedule)]
    [RequiresPermission(Permission.BookingsManage)]
    public async Task<IActionResult> RescheduleAsync([FromBody] RescheduleBookingRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _bookings.RescheduleAsync(request, cancellationToken).ConfigureAwait(false));
}