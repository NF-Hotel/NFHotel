using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The guest's way back to a booking on the public page <c>/booking</c> (BR-C-06, MIL-079): the booking number and the
/// email of the booking open the receipt.
/// </summary>
/// <remarks>
/// <b>Why the number alone is not enough.</b> The number is <c>FLZ-</c> plus the id of the booking (BR-109), so
/// <c>FLZ-000123</c> is one digit from <c>FLZ-000124</c>. Until MIL-079 the number alone opened the receipt, and anyone
/// could count through every booking and read its dates, room type and status. The email the booking was made with is
/// now the other half of the key: the guest has it, a guesser does not. The API answers a wrong email and an unknown
/// number alike and refuses a number after a few wrong emails, so the email cannot be found by trying either.
/// <para>
/// <b>Why the page still shows so little.</b> The service answers with <see cref="BookingReceiptDto"/>, which has only
/// the number, the dates, the nights, the room type and the status (BR-C-05). Even the right email brings no name,
/// phone or passport number to a public page.
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

    /// <summary>The booking number as the guest types it, or as the address gives it, for example <c>FLZ-000123</c>.</summary>
    public string BookingNumber { get; set; } = string.Empty;

    /// <summary>The email the guest booked with, in any letter case.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>The receipt, or <c>null</c> until a lookup has found the booking.</summary>
    public BookingReceiptDto? Receipt { get; private set; }

    /// <summary>True while the lookup is in progress.</summary>
    public bool IsBusy { get; private set; }

    /// <summary>The errors to show. Empty before the first lookup and after one that found the booking.</summary>
    /// <remarks>
    /// An unreadable number, an unknown number and a wrong email give the same text: the difference would only inform
    /// whoever is trying their way forward.
    /// </remarks>
    public IReadOnlyList<string> Errors { get; private set; } = NoMessages;

    /// <summary>
    /// Fills in the number from the address and forgets the last answer, so the link on the confirmation and a bookmark
    /// from before MIL-079 still take the guest to a form with the number filled in. The email is never in the address.
    /// </summary>
    /// <param name="bookingNumber">The number from the address, or <c>null</c> on <c>/booking</c>.</param>
    public void Prefill(string? bookingNumber)
    {
        BookingNumber = bookingNumber?.Trim() ?? string.Empty;
        Receipt = null;
        Errors = NoMessages;
    }

    /// <summary>
    /// Looks the booking up by <see cref="BookingNumber"/> and <see cref="Email"/>.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task LookUpAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        Receipt = null;

        if (!BookingNumberText.TryParseId(BookingNumber, out int bookingId))
        {
            // An unreadable number does not cost a lookup, and it gets the answer of a number that does not exist.
            Errors = [Describe(ErrorCodes.Booking.NotFound)];

            return;
        }

        BookingReceiptRequest request = new(bookingId, Email);

        IsBusy = true;

        try
        {
            Result<BookingReceiptDto> result = await _bookings
                .RunAsync((service, token) => service.LookUpReceiptAsync(request, token), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                IReadOnlyList<string> texts = ErrorMessages.Describe(result);
                Errors = texts.Count > 0 ? [texts[0]] : [ErrorMessages.Unexpected];

                return;
            }

            Receipt = result.Value;
            Errors = NoMessages;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Translates one error code to the text the guest reads.</summary>
    private static string Describe(string code) => ErrorMessages.Describe(new Error(code));
}
