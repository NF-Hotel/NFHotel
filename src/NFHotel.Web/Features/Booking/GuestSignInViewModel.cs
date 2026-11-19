using NFHotel.Application.Dto.Guests;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;
using NFHotel.Web.Common;

namespace NFHotel.Web.Features.Booking;

/// <summary>
/// The guest sign-in on the public page <c>/booking</c> (MIL-018, ADR-0049, UC-010): the reference and the email of a
/// booking sign the guest in to that booking, and to nothing else.
/// </summary>
/// <remarks>
/// <b>Why the reference alone is not enough.</b> The reference is random (ADR-0049 decision 1), so it cannot be worked
/// out from another booking, but it is written on a receipt that a guest may forward or lose. The email the booking was
/// made with is the other half (MIL-079): the guest has it, a finder of the receipt may not. The API answers a wrong
/// email, an unknown or unreadable reference and a cancelled booking with the same refusal, and stops a reference and an
/// address after a few failed tries (ADR-0049 decisions 2 and 6).
/// <para>
/// The view model reads nothing in the text the guest typed: the API is the one place that decides what a reference is,
/// so every try, a text that is no reference among them, is counted there. The page sets the guest cookie with the
/// session that comes back, because only the answer to a request can set a cookie.
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
    /// An unreadable reference, an unknown one, a wrong email and a cancelled booking give the same text: the difference
    /// would only inform whoever is trying their way forward.
    /// </remarks>
    public IReadOnlyList<string> Errors { get; private set; } = NoMessages;

    #endregion Properties

    #region Public methods

    /// <summary>Signs the guest in with the reference and the email of the booking.</summary>
    /// <param name="reference">The reference as the guest typed it, in any letter case.</param>
    /// <param name="email">The email the guest booked with, in any letter case.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The guest session for the page to keep in the guest cookie, or <c>null</c> with <see cref="Errors"/> set.</returns>
    public async Task<GuestSessionDto?> SignInAsync(
        string? reference,
        string? email,
        CancellationToken cancellationToken = default)
    {
        GuestSignInRequest request = new(reference, email);
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
}
