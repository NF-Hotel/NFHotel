using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Bookings;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The page of the link in the mail of a booking request, <c>/booking/verify</c> (MIL-088, ADR-0052 decisions 3 and 5):
/// its one button sends the token of the link to the API, which books the room and answers with the receipt and the
/// guest's session.
/// </summary>
/// <remarks>
/// The view model reads nothing in the token beyond its length: the API is the one place that knows a token, and it
/// gives a token that is unknown, used or lapsed one answer. A token that is missing or longer than the API reads is
/// refused here with that same answer, without a call, so the page tells a guest nothing that the API would not.
/// </remarks>
public sealed class BookingVerifyViewModel
{
    #region Fields

    private static readonly IReadOnlyList<string> NoMessages = Array.Empty<string>();

    private readonly IUseCaseRunner<IBookingRequestService> _requests;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the view model.</summary>
    /// <param name="requests">Runs use cases on <see cref="IBookingRequestService"/>.</param>
    public BookingVerifyViewModel(IUseCaseRunner<IBookingRequestService> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        _requests = requests;
    }

    #endregion Constructors

    #region Properties

    /// <summary>The errors to show. Empty before the button is pressed and after a press that booked the room.</summary>
    public IReadOnlyList<string> Errors { get; private set; } = NoMessages;

    /// <summary>True when the last press did not book the room, so the page shows why and the way back to the booking.</summary>
    public bool HasFailed => Errors.Count > 0;

    #endregion Properties

    #region Public methods

    /// <summary>Verifies the booking request of a link with its token.</summary>
    /// <param name="token">The token that the script of the page copied from the address into the form, or <c>null</c>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The guest session for the page to keep in the guest cookie, or <c>null</c> with <see cref="Errors"/> set.</returns>
    public async Task<GuestSessionDto?> VerifyAsync(string? token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(token) || token.Length > VerifyBookingRequest.MaxTokenLength)
        {
            Errors = [ErrorMessages.Describe(new Error(ErrorCodes.Booking.VerificationLinkInvalid))];
            return null;
        }

        VerifyBookingRequest request = new(token);
        Result<PublicBookingDto> result = await _requests
            .RunAsync((service, call) => service.VerifyAsync(request, call), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            Errors = [TextOf(result)];
            return null;
        }

        Errors = NoMessages;
        return result.Value.Session;
    }

    #endregion Public methods

    #region Private methods

    /// <summary>
    /// The one text of a refusal. The API answers <c>booking.not_found</c> when the verification was switched off after
    /// the mail went (ADR-0052 decision 8): the link does not work, and the guest gets the text of a link that does not.
    /// </summary>
    private static string TextOf(Result result)
    {
        Error first = result.Errors.Count > 0 ? result.Errors[0] : new Error(WebErrorCodes.Unexpected);
        string code = first.Code == ErrorCodes.Booking.NotFound ? ErrorCodes.Booking.VerificationLinkInvalid : first.Code;
        return ErrorMessages.Describe(new Error(code));
    }

    #endregion Private methods
}
