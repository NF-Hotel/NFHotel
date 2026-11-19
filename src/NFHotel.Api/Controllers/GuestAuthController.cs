using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The guest sign-in over HTTP (MIL-018, ADR-0049): the reference and the email of a booking give a guest token for that
/// booking. Public in the sense of the booking flow: it needs the token of the web application, no staff token.
/// </summary>
[ApiController]
[Route(ApiRoutes.Auth)]
public sealed class GuestAuthController : ControllerBase
{
    #region Fields

    private readonly IGuestAccessService _access;

    #endregion Fields

    #region Constructors

    /// <summary>Creates the controller.</summary>
    /// <param name="access">The guest sign-in use case.</param>
    public GuestAuthController(IGuestAccessService access)
    {
        ArgumentNullException.ThrowIfNull(access);

        _access = access;
    }

    #endregion Constructors

    #region Public methods

    /// <summary>Signs a guest in with the reference and the email of their booking.</summary>
    /// <param name="request">The reference and the email, in the body so neither stays in an access log.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>
    /// 200 with the guest session; the one 401 of <see cref="GuestAuthentication.Refusal"/> for a wrong email, an unknown
    /// or unreadable reference and a cancelled booking; 422 without an email; 429 after too many failed tries for the
    /// reference or from the address of the caller (ADR-0049 decision 6).
    /// </returns>
    [HttpPost(ApiRoutes.GuestLogin)]
    public async Task<IActionResult> SignInAsync([FromBody] GuestSignInRequest request, CancellationToken cancellationToken)
    {
        Result<GuestSessionDto> result = await _access.SignInAsync(request, cancellationToken).ConfigureAwait(false);

        // The refusal has the text of a refused guest token as well (ADR-0047 decision 3), not the envelope of a value.
        return result.Errors.Any(static error => error.Code == ErrorCodes.Api.GuestUnauthorized)
            ? GuestAuthentication.Refusal()
            : this.ToResponse(result);
    }

    #endregion Public methods
}
