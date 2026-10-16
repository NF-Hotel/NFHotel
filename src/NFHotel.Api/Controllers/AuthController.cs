using Microsoft.AspNetCore.Mvc;

using NFHotel.Api.Contracts;
using NFHotel.Application.Dto.Staff;
using NFHotel.Application.UseCases.Staff;

namespace NFHotel.Api.Controllers;

/// <summary>The sign-in of the back office over HTTP (ADR-0005). It needs the token of the web application, not a staff token.</summary>
[ApiController]
[Route(ApiRoutes.Auth)]
public sealed class AuthController : ControllerBase
{
    private readonly IStaffAuthService _auth;

    /// <summary>Creates the controller.</summary>
    /// <param name="auth">The sign-in use case.</param>
    public AuthController(IStaffAuthService auth)
    {
        ArgumentNullException.ThrowIfNull(auth);
        _auth = auth;
    }

    /// <summary>Signs a member of staff in.</summary>
    /// <param name="request">The user name and password.</param>
    /// <param name="cancellationToken">Cancels the call when the client goes away.</param>
    /// <returns>200 with the session, 401 for wrong credentials, or 429 when the attempts are used up.</returns>
    [HttpPost(ApiRoutes.Login)]
    public async Task<IActionResult> LoginAsync([FromBody] SignInRequest request, CancellationToken cancellationToken) =>
        this.ToResponse(await _auth.SignInAsync(request, cancellationToken).ConfigureAwait(false));
}