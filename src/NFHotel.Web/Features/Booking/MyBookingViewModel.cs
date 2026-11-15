using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The signed-in guest's own booking on <c>/my-booking</c> (MIL-018, ADR-0047, UC-010): the receipt, the dates to change
/// and the cancellation.
/// </summary>
/// <remarks>
/// Every call carries the guest token, and the API decides: it answers only for the booking in the token, and a guest
/// changes the dates but keeps the room type and the room (ADR-0047 decision 5). The page offers what the booking allows
/// right now (<see cref="BookingActionsDto"/>), so a guest is not shown a button the API would refuse.
/// </remarks>
public sealed class MyBookingViewModel
{
    #region Fields

    /// <summary>The word in the address after a cancellation, so a reload shows the result without cancelling again.</summary>
    public const string CancelledResult = "cancelled";

    /// <summary>The word in the address after new dates were saved.</summary>
    public const string RescheduledResult = "rescheduled";

    private static readonly IReadOnlyList<string> NoMessages = Array.Empty<string>();

    private readonly IUseCaseRunner<IBookingService> _bookings;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the view model.</summary>
    /// <param name="bookings">Runs use cases on <see cref="IBookingService"/> with the guest token of the request.</param>
    public MyBookingViewModel(IUseCaseRunner<IBookingService> bookings)
    {
        ArgumentNullException.ThrowIfNull(bookings);

        _bookings = bookings;
    }

    #endregion Constructors

    #region Properties

    /// <summary>The booking of the signed-in guest, or <c>null</c> until it is read or when it could not be read.</summary>
    public BookingDetailsDto? Booking { get; private set; }

    /// <summary>The receipt of the booking, or <c>null</c> without a booking.</summary>
    public BookingReceiptDto? Receipt => Booking is null ? null : BookingReceiptDto.From(Booking);

    /// <summary>True when the booking may be cancelled right now (BR-11).</summary>
    public bool CanCancel => Booking?.Actions.CanCancel == true;

    /// <summary>True when the dates of the booking may be changed right now (BR-14).</summary>
    public bool CanReschedule => Booking?.Actions.CanReschedule == true;

    /// <summary>The result of the last change, or <c>null</c>.</summary>
    public string? Notice { get; private set; }

    /// <summary>The errors to show; empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = NoMessages;

    #endregion Properties

    #region Public methods

    /// <summary>Reads the booking of the signed-in guest.</summary>
    /// <param name="bookingId">The key of the booking in the guest cookie.</param>
    /// <param name="result">The word of the last change from the address, or <c>null</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the booking is read or the errors are set.</returns>
    public async Task LoadAsync(int bookingId, string? result, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bookingId);

        Result<BookingDetailsDto> read = await _bookings
            .RunAsync((service, token) => service.GetByIdAsync(bookingId, token), cancellationToken)
            .ConfigureAwait(false);

        Booking = read.IsSuccess ? read.Value : null;
        Errors = read.IsSuccess ? NoMessages : TextsOf(read);
        Notice = read.IsSuccess ? NoticeFor(result) : null;
    }

    /// <summary>Cancels the booking.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True when the booking was cancelled; false with <see cref="Errors"/> set.</returns>
    public async Task<bool> CancelAsync(CancellationToken cancellationToken = default)
    {
        if (Booking is not BookingDetailsDto booking)
        {
            return false;
        }

        int bookingId = booking.BookingId;
        Result<BookingDetailsDto> cancelled = await _bookings
            .RunAsync((service, token) => service.CancelAsync(bookingId, token), cancellationToken)
            .ConfigureAwait(false);

        return Apply(cancelled);
    }

    /// <summary>Moves the booking to new dates, in the room it has.</summary>
    /// <param name="checkIn">The new arrival date.</param>
    /// <param name="checkOut">The new departure date.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True when the new dates were saved; false with <see cref="Errors"/> set.</returns>
    public async Task<bool> RescheduleAsync(DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken = default)
    {
        if (Booking is not BookingDetailsDto booking)
        {
            return false;
        }

        RescheduleBookingRequest request = new(booking.BookingId, checkIn, checkOut, booking.Room.RoomId);
        Result<BookingDetailsDto> moved = await _bookings
            .RunAsync((service, token) => service.RescheduleAsync(request, token), cancellationToken)
            .ConfigureAwait(false);

        return Apply(moved);
    }

    #endregion Public methods

    #region Private methods

    /// <summary>The text of a change that the address names, or <c>null</c> for anything else.</summary>
    private static string? NoticeFor(string? result) => result switch
    {
        CancelledResult => "Your booking is cancelled.",
        RescheduledResult => "Your new dates are saved.",
        _ => null,
    };

    private static IReadOnlyList<string> TextsOf(Result result)
    {
        IReadOnlyList<string> texts = ErrorMessages.Describe(result);
        return texts.Count > 0 ? texts : [ErrorMessages.Unexpected];
    }

    /// <summary>Keeps the booking after a change, or the errors of a refusal and the booking as it was.</summary>
    private bool Apply(Result<BookingDetailsDto> changed)
    {
        Notice = null;
        if (changed.IsFailure)
        {
            Errors = TextsOf(changed);
            return false;
        }

        Booking = changed.Value;
        Errors = NoMessages;
        return true;
    }

    #endregion Private methods
}
