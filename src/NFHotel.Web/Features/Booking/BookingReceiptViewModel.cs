using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// Looks up one booking by its booking number for the public receipt page
/// <c>/booking/{bookingNumber}</c> (BR-C-06).
/// </summary>
/// <remarks>
/// <b>Why the page shows so little.</b> There is no access control in Phase 1 (Phase1-Scope.md),
/// and the booking number is all it takes to open the page. The format is
/// <c>FLZ-</c> plus the id of the booking (BR-109), so <c>FLZ-000123</c> is one digit away from
/// <c>FLZ-000124</c>: whoever guesses hits another guest's booking on the first try.
/// <para>
/// Therefore the page carries only what a guesser cannot use for anything — booking number,
/// dates, number of nights, room and status (BR-C-05). Name, email, phone and passport number
/// are <b>not</b> shown, even though <see cref="BookingDetailsDto.Guest"/> has them: the customer has
/// just entered them and already has them, whereas a stranger could otherwise harvest
/// personal data by counting upwards. It is a deliberate limitation until Identity arrives, and
/// it must be lifted only together with the access control — not before.
/// </para>
/// <para>
/// The limitation is enforced by <c>BookingReceiptCard.razor</c>, which has markup only for the
/// allowed fields. The viewmodel carries the whole DTO, because it comes that way from the service.
/// </para>
/// </remarks>
public sealed class BookingReceiptViewModel
{
    private static readonly IReadOnlyList<string> NoMessages = Array.Empty<string>();

    private readonly IUseCaseRunner<IBookingService> _bookings;

    /// <summary>
    /// Creates the view model.
    /// </summary>
    /// <param name="bookings">Runs use cases on <see cref="IBookingService"/>.</param>
    public BookingReceiptViewModel(IUseCaseRunner<IBookingService> bookings)
    {
        ArgumentNullException.ThrowIfNull(bookings);

        _bookings = bookings;
    }

    /// <summary>The booking, or <c>null</c> if it was not found.</summary>
    public BookingDetailsDto? Details { get; private set; }

    /// <summary>True while the lookup is in progress.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>True when the lookup has been tried at least once.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>The Danish explanation of why there is no booking.</summary>
    /// <remarks>
    /// The same text whether the number was nonsense or pointed to a booking that does not
    /// exist. The difference would only inform whoever is trying their way forward.
    /// </remarks>
    public string NotFoundText { get; private set; } = string.Empty;

    /// <summary>
    /// Looks up the booking.
    /// </summary>
    /// <param name="bookingNumber">The booking number from the URL, for example <c>FLZ-000123</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task LoadAsync(string? bookingNumber, CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        if (!BookingNumberText.TryParseId(bookingNumber, out var bookingId))
        {
            // An unreadable number does not cost a database lookup.
            Details = null;
            NotFoundText = Describe(ErrorCodes.Booking.NotFound);
            IsLoaded = true;

            return;
        }

        IsBusy = true;

        try
        {
            var result = await _bookings
                .RunAsync((service, token) => service.GetByIdAsync(bookingId, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                Details = null;
                NotFoundText = ErrorMessages.Describe(result) is [var first, ..]
                    ? first
                    : ErrorMessages.Unexpected;

                return;
            }

            Details = result.Value;
            NotFoundText = string.Empty;
        }
        finally
        {
            IsBusy = false;
            IsLoaded = true;
        }
    }

    /// <summary>The errors to show. Empty when the booking was found.</summary>
    public IReadOnlyList<string> Errors =>
        IsLoaded && Details is null && NotFoundText.Length > 0 ? [NotFoundText] : NoMessages;

    /// <summary>Translates one error code to Danish.</summary>
    private static string Describe(string code) => ErrorMessages.Describe(new Error(code));
}
