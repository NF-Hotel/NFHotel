using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Api.Security;
using NFHotel.Application.Dto.Bookings;
using NFHotel.Application.Dto.Guests;
using NFHotel.Application.Errors;
using NFHotel.Application.UseCases;
using NFHotel.Application.UseCases.Guests;

namespace NFHotel.Api.Controllers;

/// <summary>
/// The guest sign-in over HTTP (MIL-018, ADR-0047): the number and the email of a booking give a guest token for that
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

    /// <summary>Signs a guest in with the number and the email of their booking.</summary>
    /// <param name="request">The booking key and the email, in the body so the email stays out of access logs.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>
    /// 200 with the guest session; the one 401 of <see cref="GuestAuthentication.Refusal"/> for a wrong email, an unknown
    /// number and a cancelled booking; 422 without an email; 429 after too many failed attempts for the number.
    /// </returns>
    [HttpPost(ApiRoutes.GuestLogin)]
    public async Task<IActionResult> SignInAsync([FromBody] BookingReceiptRequest request, CancellationToken cancellationToken)
    {
        Result<GuestSessionDto> result = await _access.SignInAsync(request, cancellationToken).ConfigureAwait(false);

        // The refusal has the text of a refused guest token as well (ADR-0047 decision 3), not the envelope of a value.
        return result.Errors.Any(static error => error.Code == ErrorCodes.Api.GuestUnauthorized)
            ? GuestAuthentication.Refusal()
            : this.ToResponse(result);
    }

    #endregion Public methods
}
