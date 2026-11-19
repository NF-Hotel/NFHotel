using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Ports;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Domain.ValueObjects;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The signed-in guest's own booking on <c>/my-booking</c> (MIL-018, ADR-0049, UC-010): the receipt, the dates to change
/// and the cancellation.
/// </summary>
/// <remarks>
/// Every call carries the guest token and names no booking: the API answers for the booking in the token, with the
/// receipt alone, and a guest changes the dates but keeps the room type and the room (ADR-0047 decision 5, ADR-0049
/// decisions 3 and 7). The page offers the two changes for an open booking only, by the rule of the domain, so a guest is
/// not shown a button the API would refuse.
/// </remarks>
public sealed class MyBookingViewModel
{
    #region Fields

    /// <summary>The word in the address after a cancellation, so a reload shows the result without cancelling again.</summary>
    public const string CancelledResult = "cancelled";

    /// <summary>The word in the address after new dates were saved.</summary>
    public const string RescheduledResult = "rescheduled";

    private static readonly IReadOnlyList<string> NoMessages = Array.Empty<string>();

    private readonly IUseCaseRunner<IGuestBookingService> _booking;
    private readonly IClock _clock;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the view model.</summary>
    /// <param name="booking">Runs use cases on <see cref="IGuestBookingService"/> with the guest token of the request.</param>
    /// <param name="clock">Gives the date of today for the limits of the new dates (MIL-086).</param>
    public MyBookingViewModel(IUseCaseRunner<IGuestBookingService> booking, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(booking);
        ArgumentNullException.ThrowIfNull(clock);

        _booking = booking;
        _clock = clock;
    }

    #endregion Constructors

    #region Properties

    /// <summary>The first arrival that the form offers (MIL-086 skeleton: not set yet).</summary>
    public DateOnly EarliestCheckIn => _clock.Today;

    /// <summary>The last arrival that the form offers (MIL-086 skeleton: not set yet).</summary>
    public DateOnly LatestCheckIn => default;

    /// <summary>The first departure that the form offers (MIL-086 skeleton: not set yet).</summary>
    public DateOnly EarliestCheckOut => default;

    /// <summary>The last departure that the form offers (MIL-086 skeleton: not set yet).</summary>
    public DateOnly LatestCheckOut => default;

    /// <summary>The receipt of the guest's booking, or <c>null</c> until it is read or when it could not be read.</summary>
    public BookingReceiptDto? Receipt { get; private set; }

    /// <summary>True when the booking may be cancelled: it is open (BR-11).</summary>
    public bool CanCancel => Receipt?.Status.IsOpen() == true;

    /// <summary>True when the dates of the booking may be changed: it is open (BR-14).</summary>
    public bool CanReschedule => Receipt?.Status.IsOpen() == true;

    /// <summary>The result of the last change, or <c>null</c>.</summary>
    public string? Notice { get; private set; }

    /// <summary>The errors to show; empty when the last call succeeded.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = NoMessages;

    #endregion Properties

    #region Public methods

    /// <summary>Reads the receipt of the signed-in guest's booking.</summary>
    /// <param name="result">The word of the last change from the address, or <c>null</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the receipt is read or the errors are set.</returns>
    public async Task LoadAsync(string? result, CancellationToken cancellationToken = default)
    {
        Result<BookingReceiptDto> read = await _booking
            .RunAsync((service, token) => service.GetAsync(token), cancellationToken)
            .ConfigureAwait(false);

        Receipt = read.IsSuccess ? read.Value : null;
        Errors = read.IsSuccess ? NoMessages : TextsOf(read);
        Notice = read.IsSuccess ? NoticeFor(result) : null;
    }

    /// <summary>Cancels the booking.</summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True when the booking was cancelled; false with <see cref="Errors"/> set.</returns>
    public async Task<bool> CancelAsync(CancellationToken cancellationToken = default)
    {
        if (Receipt is null)
        {
            return false;
        }

        Result<BookingReceiptDto> cancelled = await _booking
            .RunAsync((service, token) => service.CancelAsync(token), cancellationToken)
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
        if (Receipt is null)
        {
            return false;
        }

        GuestRescheduleRequest request = new(checkIn, checkOut);
        Result<BookingReceiptDto> moved = await _booking
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

    /// <summary>Keeps the receipt after a change, or the errors of a refusal and the receipt as it was.</summary>
    private bool Apply(Result<BookingReceiptDto> changed)
    {
        Notice = null;
        if (changed.IsFailure)
        {
            Errors = TextsOf(changed);
            return false;
        }

        Receipt = changed.Value;
        Errors = NoMessages;
        return true;
    }

    #endregion Private methods
}
