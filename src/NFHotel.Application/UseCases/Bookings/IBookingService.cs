using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Rooms;
using NFHotel.Application.Errors;

namespace NFHotel.Application.UseCases.Bookings;

/// <summary>
/// Use cases for bookings. Carries the most business rules of the three services.
/// </summary>
/// <remarks>
/// The test for every single method: <i>can a console app call it and get every rule
/// enforced?</i> No UI types, no dialogs, no <c>Debug.WriteLine</c> — 823 lines of
/// logic in one ViewModel was the core fault of the old system (B-10).
/// <para>
/// Note the placement of <see cref="GetAvailableRoomsAsync"/>: it returns rooms, but
/// the rule is a <i>booking rule</i> (overlap in a period). If it lived on <c>IRoomService</c>,
/// it would have to know <c>IBookingRepository</c>, and then both services would have both repositories.
/// </para>
/// <para>
/// There is no <c>SalesService</c> in Phase 1: BR-31 always sets revenue to 0, and there
/// is no price in the domain. A service that returns 0 is worse than none — it looks like
/// en implementering.
/// </para>
/// </remarks>
public interface IBookingService
{
    /// <summary>
    /// Fetches one booking (BR-115).
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The details of the booking, or <see cref="ErrorCodes.Booking.NotFound"/>.</returns>
    Task<Result<BookingDetailsDto>> GetByIdAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetches the booking overview (BR-23, BR-24, BR-25, BR-32, BR-111).
    /// </summary>
    /// <param name="query">Date window, free text, sorting and whether cancelled bookings are included.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The rows of the overview, sorted as requested.</returns>
    /// <remarks>
    /// The period filter is always active. It fixes the fault where the week and month views did not
    /// filter at all and therefore fetched the whole table (BR-111).
    /// </remarks>
    Task<Result<IReadOnlyList<BookingListItemDto>>> GetOverviewAsync(
        BookingOverviewQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds free rooms in a period (BR-34, BR-36, BR-37, BR-38, BR-39, BR-40, BR-110).
    /// </summary>
    /// <param name="checkInDate">Arrival date, inclusive.</param>
    /// <param name="checkOutDate">Departure date, exclusive.</param>
    /// <param name="excludeBookingId">
    /// The booking being edited, so it does not block itself. <c>null</c> on creation.
    /// The parameter was missing in the old system and made BR-19 needlessly complicated.
    /// </param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The rooms that are both bookable (BR-110) and free for the whole period.</returns>
    Task<Result<IReadOnlyList<AvailableRoomDto>>> GetAvailableRoomsAsync(
        DateOnly checkInDate,
        DateOnly checkOutDate,
        int? excludeBookingId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a booking (BR-01, BR-19, BR-41 to BR-49, BR-51, BR-101 to BR-106, B-03).
    /// </summary>
    /// <param name="request">Period, room and either an existing or a new guest (BR-47).</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The created booking, or the broken rules.</returns>
    /// <remarks>
    /// The overlap check is <b>new</b> — it existed neither in the old system's
    /// <c>CreateBooking</c> nor in the repo, so double booking was possible (B-03).
    /// </remarks>
    Task<Result<BookingDetailsDto>> CreateAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms a booking — payment is guaranteed (BR-02, BR-03, BR-05, BR-121).
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The booking, or <see cref="ErrorCodes.Booking.ConfirmNotAllowed"/>.</returns>
    /// <remarks>The confirmation dialog (BR-04) is UI and belongs in Web.</remarks>
    Task<Result<BookingDetailsDto>> ConfirmAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the guest in (BR-06, BR-07, BR-08).
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The booking, or <see cref="ErrorCodes.Booking.CheckInNotAllowed"/>.</returns>
    /// <remarks>
    /// Walk-in is allowed: <c>Pending</c> goes directly to <c>CheckedIn</c> bypassing
    /// <c>Confirmed</c> (B-01). The time comes from the clock, never from <c>DateTime.Now</c> (A-05).
    /// </remarks>
    Task<Result<BookingDetailsDto>> CheckInAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the guest out (BR-09, BR-10, BR-107, A-01, A-03, BR-N-06).
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The booking, or <see cref="ErrorCodes.Booking.CheckOutNotAllowed"/>.</returns>
    /// <remarks>
    /// Sets the hotel-local check-out date, which releases the remaining nights on early
    /// check-out without touching the booked end date (A-01), and sets the room to
    /// departure cleaning (BR-N-06).
    /// </remarks>
    Task<Result<BookingDetailsDto>> CheckOutAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a booking (BR-11, BR-13, BR-122).
    /// </summary>
    /// <param name="bookingId">Bookingens id.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The booking, or <see cref="ErrorCodes.Booking.CancelNotAllowed"/>.</returns>
    /// <remarks>
    /// Soft delete: the status is set, the row stays (BR-13). It is the only transition that
    /// releases the room completely (A-01).
    /// </remarks>
    Task<Result<BookingDetailsDto>> CancelAsync(int bookingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a booking to new dates and/or a new room
    /// (BR-14, BR-17, BR-18, BR-19, BR-20, BR-108, BR-123, B-03).
    /// </summary>
    /// <param name="request">The id of the booking, the new period and the room.</param>
    /// <param name="cancellationToken">Annulleringstoken.</param>
    /// <returns>The moved booking, or the broken rules.</returns>
    /// <remarks>
    /// Uses the same overlap function as <see cref="CreateAsync"/>, which closes
    /// the contradiction between BR-19 and BR-39.
    /// </remarks>
    Task<Result<BookingDetailsDto>> RescheduleAsync(
        RescheduleBookingRequest request,
        CancellationToken cancellationToken = default);
}