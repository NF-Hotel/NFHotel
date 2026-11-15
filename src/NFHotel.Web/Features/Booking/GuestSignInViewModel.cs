using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The guest sign-in on the public page <c>/booking</c> (MIL-018, ADR-0047, UC-010): the number and the email of a
/// booking sign the guest in to that booking, and to nothing else.
/// </summary>
/// <remarks>
/// <b>Why the number alone is not enough.</b> The number is <c>FLZ-</c> plus the key of the booking (BR-109), so
/// <c>FLZ-000123</c> is one digit from <c>FLZ-000124</c>. The email the booking was made with is the other half of the
/// key (MIL-079): the guest has it, a guesser does not. The API answers a wrong email, an unknown number and a
/// cancelled booking with the same refusal, and refuses a number after a few failed attempts, so neither the number
/// nor the email can be found by trying.
/// <para>
/// The view model only asks: the page sets the guest cookie with the session it returns, because only the answer to a
/// request can set a cookie.
/// </para>
/// </remarks>
public sealed class GuestSignInViewModel
{
    #region Fields

    private static readonly IReadOnlyList<string> NoMessages = Array.Empty<string>();

    private readonly IUseCaseRunner<IGuestAccessService> _access;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the view model.</summary>
    /// <param name="access">Runs use cases on <see cref="IGuestAccessService"/>.</param>
    public GuestSignInViewModel(IUseCaseRunner<IGuestAccessService> access)
    {
        ArgumentNullException.ThrowIfNull(access);

        _access = access;
    }

    #endregion Constructors

    #region Properties

    /// <summary>The errors to show. Empty before the first attempt and after one that signed the guest in.</summary>
    /// <remarks>
    /// An unreadable number, an unknown number, a wrong email and a cancelled booking give the same text: the difference
    /// would only inform whoever is trying their way forward.
    /// </remarks>
    public IReadOnlyList<string> Errors { get; private set; } = NoMessages;

    #endregion Properties

    #region Public methods

    /// <summary>Signs the guest in with the number and the email of the booking.</summary>
    /// <param name="bookingNumber">The booking number as the guest typed it, for example <c>FLZ-000123</c>.</param>
    /// <param name="email">The email the guest booked with, in any letter case.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The guest session for the page to keep in the guest cookie, or <c>null</c> with <see cref="Errors"/> set.</returns>
    public async Task<GuestSessionDto?> SignInAsync(
        string? bookingNumber,
        string? email,
        CancellationToken cancellationToken = default)
    {
        if (!BookingNumberText.TryParseId(bookingNumber, out int bookingId))
        {
            // An unreadable number does not cost an attempt, and it gets the answer of a number that does not exist.
            Errors = [Describe(ErrorCodes.Api.GuestUnauthorized)];
            return null;
        }

        BookingReceiptRequest request = new(bookingId, email);
        Result<GuestSessionDto> result = await _access
            .RunAsync((service, token) => service.SignInAsync(request, token), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            IReadOnlyList<string> texts = ErrorMessages.Describe(result);
            Errors = texts.Count > 0 ? [texts[0]] : [ErrorMessages.Unexpected];
            return null;
        }

        Errors = NoMessages;
        return result.Value;
    }

    #endregion Public methods

    #region Private methods

    /// <summary>Translates one error code to the text the guest reads.</summary>
    private static string Describe(string code) => ErrorMessages.Describe(new Error(code));

    #endregion Private methods
}
